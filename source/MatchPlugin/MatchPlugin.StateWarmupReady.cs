/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class StateWarmupReady(Match match) : StateWarmup(match)
{
    public static readonly List<string> ReadyCmds = ["css_ready", "css_r"];
    public static readonly List<string> UnreadyCmds = ["css_unready", "css_ur"];

    public override void Load()
    {
        base.Load();

        Match.Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        Match.Plugin.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Match.Plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Match.Plugin.CreateChatTimer("PrintWarmupCommands", OnPrintWarmupCommands);
        ReadyCmds.ForEach(c => Match.Plugin.AddCommand(c, "Mark as ready.", OnReadyCommand));
        UnreadyCmds.ForEach(c => Match.Plugin.AddCommand(c, "Mark as unready.", OnUnreadyCommand));

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
            player.IsReady = false;

        Config.ExecWarmup();
    }

    public override void Unload()
    {
        base.Unload();
        Match.Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        Match.Plugin.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Match.Plugin.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Match.Plugin.ClearTimer("PrintWarmupCommands");
        ReadyCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnReadyCommand));
        UnreadyCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnUnreadyCommand));
    }

    public void OnTick()
    {
        if (Match.State is not StateWarmupReady)
            return;
        foreach (var controller in UtilitiesX.GetPlayersInTeams())
            if (!controller.IsBot)
                controller.SetClan(
                    Match.Plugin.Localizer[
                        Match.GetPlayerFromSteamID(controller.SteamID)?.IsReady == true
                            ? "match.ready"
                            : "match.not_ready"
                    ]
                );
    }

    public void OnPrintWarmupCommands()
    {
        var needed =
            Match.players_needed.Value
            - Match.Teams.SelectMany(t => t.Players).Count(p => p.IsReady);
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

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo _)
    {
        if (!Match.LoadedFromFile)
            Match.RemovePlayerBySteamID(@event.Userid?.SteamID);
        return HookResult.Continue;
    }

    public void OnReadyCommand(CCSPlayerController? controller, CommandInfo _)
    {
        if (controller != null)
        {
            var player = Match.GetPlayerFromSteamID(controller.SteamID);
            if (player == null && !Match.LoadedFromFile)
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
                CheckIfPlayersAreReady();
            }
        }
    }

    public void OnUnreadyCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null)
            player.IsReady = false;
    }

    public void CheckIfPlayersAreReady()
    {
        var players = Match.Teams.SelectMany(t => t.Players);
        if (players.Count() == Match.players_needed.Value && players.All(p => p.IsReady))
        {
            var idsInMatch = players.Select(p => p.SteamID);
            foreach (var controller in UtilitiesX.GetPlayersInTeams())
                if (idsInMatch.Contains(controller.SteamID))
                    controller.SetClan("");
                else
                    controller.ChangeTeam(CsTeam.Spectator);
            Match.SetState<StateKnifeRound>();
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        if (!Match.LoadedFromFile)
            Match.RemovePlayerBySteamID(@event.Userid?.SteamID);
        return HookResult.Continue;
    }
}
