﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace MatchPlugin;

public class StateWarmupKnifeVote : StateWarmup
{
    public override string Name => "waiting_for_knife_decision";

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
        StayCmds.ForEach(c => AddCommand(c, "Stay in current team.", OnStayCommand));
        SwitchCmds.ForEach(c => AddCommand(c, "Switch current team", OnSwitchCommand));
        Match.Plugin.CreateChatTimer("PrintKnifeVoteCommands", OnPrintKnifeVoteCommands);
        Match.Plugin.CreateTimer(
            "KnifeVoteTimeout",
            Match.knife_vote_timeout.Value - 1,
            OnKnifeVoteTimeout
        );

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
            player.KnifeRoundVote = KnifeRoundVote.None;

        Match.Log("Execing Knife Vote");
        Config.ExecWarmup(warmupTime: Match.knife_vote_timeout.Value, lockTeams: true);
    }

    public override void Unload()
    {
        base.Unload();
        RemoveAllCommands();

        Match.Plugin.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeamPre, HookMode.Pre);
        Match.Plugin.ClearTimer("PrintKnifeVoteCommands");
        Match.Plugin.ClearTimer("KnifeVoteTimeout");
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
                    team.FormattedName,
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
                Match.Log($"{controller?.PlayerName} voted !stay.");
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
                Match.Log($"{controller?.PlayerName} voted !switch.");
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
                    Match.Log("Leader has decided a side.");
                    ProcessKnifeVote(vote);
                    return;
                }
    }

    public void OnKnifeVoteTimeout()
    {
        Match.Log("Knive vote has timed out");
        ProcessKnifeVote(KnifeRoundVote.None);
    }

    public void ProcessKnifeVote(KnifeRoundVote decision)
    {
        Match.Log($"decision={decision}");
        var winnerTeam = Match.KnifeRoundWinner;
        if (winnerTeam == null)
            return;
        if (decision != KnifeRoundVote.None)
        {
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
                    winnerTeam.FormattedName,
                    decisionLabel
                ]
            );
        }
        if (decision == KnifeRoundVote.Switch)
        {
            foreach (var team in Match.Teams)
                team.StartingTeam = UtilitiesX.ToggleCsTeam(team.StartingTeam);
            UtilitiesX.GetGameRules().HandleSwapTeams();
        }

        Match.SendEvent(Match.Get5.OnSidePicked(team: winnerTeam));
        Match.SendEvent(Match.Get5.OnKnifeRoundWon(team: winnerTeam, decision));
        Match.SetState(new StateLive());
    }
}
