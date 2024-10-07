/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace MatchPlugin;

public class StateWarmupReady : StateWarmup
{
    public static readonly List<string> ReadyCmds = ["css_ready", "css_r", "css_pronto"];
    public static readonly List<string> UnreadyCmds = ["css_unready", "css_ur", "css_naopronto"];

    private long _warmupStart = 0;

    public override void Load()
    {
        Match.Log($"matchmaking={Match.IsMatchmaking()}");
        Match.Cstv.Stop();
        Match.Cstv.Set(Match.tv_record.Value);

        if (Match.CheckCurrentMap())
            return /* Map will be changed. */
            ;

        base.Load();

        Match.Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        Match.Plugin.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Match.Plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Match.Plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch);

        ReadyCmds.ForEach(c => AddCommand(c, "Mark as ready.", OnReadyCommand));
        UnreadyCmds.ForEach(c => AddCommand(c, "Mark as unready.", OnUnreadyCommand));

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
        {
            player.IsReady = false;
            player.Stats = new(player.SteamID);
        }

        if (Match.IsMatchmaking())
        {
            _warmupStart = ServerX.Now();
            Match.Plugin.CreateSecondIntervalTimer(
                "PrintWaitingPlayersReady",
                PrintMatchmakingReady
            );
            Match.Plugin.CreateTimer(
                "MatchmakingReadyTimeout",
                Match.matchmaking_ready_timeout.Value,
                () => OnMatchCancelled()
            );
        }

        Match.Plugin.CreateChatTimer("PrintWarmupCommands", OnPrintWarmupCommands);

        Match.Log("Execing Warmup Ready");
        Config.ExecWarmup(
            warmupTime: Match.IsMatchmaking() ? Match.matchmaking_ready_timeout.Value : -1,
            lockTeams: Match.AreTeamsLocked()
        );

        _matchCancelled = false;
    }

    public override void Unload()
    {
        base.Unload();
        RemoveAllCommands();

        Match.Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        Match.Plugin.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Match.Plugin.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Match.Plugin.DeregisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        Match.Plugin.ClearTimer("PrintWarmupCommands");
        Match.Plugin.ClearTimer("PrintWaitingPlayersReady");
        Match.Plugin.ClearTimer("MatchmakingReadyTimeout");
    }

    public void OnTick()
    {
        if (Match.State is not StateWarmupReady)
            return;
        bool didUpdateControllers = false;
        foreach (var controller in UtilitiesX.GetPlayersInTeams())
            if (
                !controller.IsBot
                && UtilitiesX.SetPlayerClan(
                    controller,
                    Match.Plugin.Localizer[
                        Match.GetPlayerFromSteamID(controller.SteamID)?.IsReady == true
                            ? "match.ready"
                            : "match.not_ready"
                    ]
                )
            )
                didUpdateControllers = true;
        if (didUpdateControllers)
            ServerX.UpdatePlayersScoreboard();
    }

    public void OnPrintWarmupCommands()
    {
        var needed =
            Match.GetNeededPlayers() - Match.Teams.SelectMany(t => t.Players).Count(p => p.IsReady);
        foreach (var controller in UtilitiesX.GetPlayersInTeams())
        {
            var localize = Match.Plugin.Localizer;
            var player = Match.GetPlayerFromSteamID(controller.SteamID);
            controller.PrintToChat(localize["match.commands", Match.GetChatPrefix()]);
            if (needed > 0)
                controller.PrintToChat(localize["match.commands_needed", needed]);
            if (player?.IsReady != true)
                controller.PrintToChat(localize["match.commands_ready"]);
            controller.PrintToChat(localize["match.commands_gg"]);
        }
    }

    public void PrintMatchmakingReady()
    {
        var timeleft = Math.Max(
            0,
            Match.matchmaking_ready_timeout.Value - (ServerX.Now() - _warmupStart)
        );
        if (timeleft % 30 != 0)
            return;
        var formattedTimeleft = UtilitiesX.FormatTimeString(timeleft);
        var unreadyTeams = Match.Teams.Where(t => t.Players.Any(p => !p.IsReady));
        if (timeleft == 0)
            Match.Plugin.ClearTimer("PrintWaitingPlayersReady");
        else
            switch (unreadyTeams.Count())
            {
                case 1:
                    var team = unreadyTeams.First();
                    Server.PrintToChatAll(
                        Match.Plugin.Localizer[
                            "match.match_waiting_team",
                            Match.GetChatPrefix(stripColors: true),
                            team.FormattedName,
                            formattedTimeleft
                        ]
                    );
                    break;

                case 2:
                    Server.PrintToChatAll(
                        Match.Plugin.Localizer[
                            "match.match_waiting_players",
                            Match.GetChatPrefix(stripColors: true),
                            formattedTimeleft
                        ]
                    );
                    break;
            }
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo _)
    {
        if (!Match.IsLoadedFromFile)
            Match.RemovePlayerBySteamID(@event.Userid?.SteamID);
        return HookResult.Continue;
    }

    public void OnReadyCommand(CCSPlayerController? controller, CommandInfo _)
    {
        if (controller != null && !_matchCancelled)
        {
            Match.Log($"{controller.PlayerName} sent !ready.");
            var player = Match.GetPlayerFromSteamID(controller.SteamID);
            if (player == null && !Match.IsLoadedFromFile)
            {
                var team = Match.GetTeamFromCsTeam(controller.Team);
                if (team != null && team.CanAddPlayer())
                {
                    player = new(controller.SteamID, controller.PlayerName, team, controller);
                    team.AddPlayer(player);
                }
            }
            if (player != null)
            {
                player.IsReady = true;
                TryStartKnifeRound();
            }
        }
    }

    public void OnUnreadyCommand(CCSPlayerController? controller, CommandInfo _)
    {
        Match.Log($"{controller?.PlayerName} sent !unready.");
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null)
            player.IsReady = false;
    }

    public void TryStartKnifeRound()
    {
        var players = Match.Teams.SelectMany(t => t.Players);
        if (players.Count() == Match.GetNeededPlayers() && players.All(p => p.IsReady))
        {
            Match.Setup();
            Match.Log("Starting knife round from ready.");
            Match.SetState(new StateKnifeRound());
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        if (!Match.IsLoadedFromFile)
            Match.RemovePlayerBySteamID(@event.Userid?.SteamID);
        return HookResult.Continue;
    }
}
