/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class StateWarmupKnifeVote(Match match) : StateWarmup(match)
{
    public static readonly List<string> StayCmds = ["css_stay", "css_ficar"];
    public static readonly List<string> SwitchCmds = ["css_switch", "css_trocar"];
    public static readonly List<KnifeRoundVote> KnifeRoundVotes =
    [
        KnifeRoundVote.Stay,
        KnifeRoundVote.Switch
    ];

    public override void Load()
    {
        base.Load();

        Match.Plugin.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeamPre, HookMode.Pre);
        StayCmds.ForEach(c => Match.Plugin.AddCommand(c, "Stay in current team.", OnStayCommand));
        SwitchCmds.ForEach(c => Match.Plugin.AddCommand(c, "Switch current team", OnSwitchCommand));
        Match.Plugin.CreateChatTimer("PrintKnifeVoteCommands", OnPrintKnifeVoteCommands);

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
            player.KnifeRoundVote = KnifeRoundVote.None;

        Config.ExecWarmup(warmupTime: 60);
    }

    public override void Unload()
    {
        base.Unload();

        Match.Plugin.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeamPre, HookMode.Pre);
        StayCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnStayCommand));
        SwitchCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnSwitchCommand));
        Match.Plugin.ClearTimer("PrintKnifeVoteCommands");
    }

    public HookResult OnPlayerTeamPre(EventPlayerTeam @event, GameEventInfo _)
    {
        return HookResult.Stop;
    }

    public void OnPrintKnifeVoteCommands()
    {
        var team = Match.KnifeRoundWinner;
        var leader = team?.InGameLeader;
        if (team != null && leader != null)
            Server.PrintToChatAll(
                Match.Plugin.Localizer[
                    "match.knife_vote",
                    Match.GetChatPrefix(),
                    team.GetName(),
                    leader.Name
                ]
            );
    }

    public void OnStayCommand(CCSPlayerController? controller, CommandInfo _)
    {
        if (controller != null)
        {
            var player = Match.GetPlayerFromSteamID(controller.SteamID);
            if (player != null)
            {
                player.KnifeRoundVote = KnifeRoundVote.Stay;
                CheckIfPlayersVoted();
            }
        }
    }

    public void OnSwitchCommand(CCSPlayerController? controller, CommandInfo _)
    {
        if (controller != null)
        {
            var player = Match.GetPlayerFromSteamID(controller.SteamID);
            if (player != null)
            {
                player.KnifeRoundVote = KnifeRoundVote.Switch;
                CheckIfPlayersVoted();
            }
        }
    }

    public void CheckIfPlayersVoted()
    {
        var team = Match.KnifeRoundWinner;
        if (team != null)
            foreach (var vote in KnifeRoundVotes)
                if (
                    team
                        .Players.Where(p =>
                            p.KnifeRoundVote == vote && p.SteamID == team.InGameLeader?.SteamID
                        )
                        .Any()
                )
                {
                    ProcessKnifeVote(vote);
                    return;
                }
    }

    public void ProcessKnifeVote(KnifeRoundVote decision)
    {
        var winnerTeam = Match.KnifeRoundWinner;
        if (winnerTeam == null)
            return;

        var localize = Match.Plugin.Localizer;
        var decisionLabel = localize[
            decision == KnifeRoundVote.Switch
                ? "match.knife_decision_switch"
                : "match.knife_decision_stay"
        ];
        Server.PrintToChatAll(
            localize[
                "match.knife_decision",
                Match.GetChatPrefix(),
                winnerTeam.GetName(),
                decisionLabel
            ]
        );
        if (decision == KnifeRoundVote.Switch)
        {
            foreach (var team in Match.Teams)
            {
                team.StartingTeam = UtilitiesX.ToggleCsTeam(team.StartingTeam);
                ServerX.SetTeamName(team.StartingTeam, team.GetServerName());
                foreach (var player in team.Players)
                    player.Controller?.SwitchTeam(team.StartingTeam);
            }
        }
        Server.NextFrame(Match.SetState<StateLive>);
    }
}
