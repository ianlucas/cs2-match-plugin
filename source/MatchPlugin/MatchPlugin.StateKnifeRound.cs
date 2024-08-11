/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public class StateKnifeRound(Match match) : State(match)
{
    public override void Load()
    {
        Match.KnifeRoundWinner = null;
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.RegisterEventHandler<EventRoundMvp>(OnRoundMvpPre, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);

        Config.ExecKnife();

        Match.Cstv.Record(Match.GetDemoFilename());
    }

    public override void Unload()
    {
        Match.Plugin.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.DeregisterEventHandler<EventRoundMvp>(OnRoundMvpPre, HookMode.Pre);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        if (Match.KnifeRoundWinner != null)
        {
            Match.SetState<StateWarmupKnifeVote>();
        }
        else
            ServerX.PrintToChatAllRepeat(
                Match.Plugin.Localizer["match.knife", Match.GetChatPrefix()]
            );
        return HookResult.Continue;
    }

    public HookResult OnRoundMvpPre(EventRoundMvp @event, GameEventInfo _)
    {
        // @todo: Don't work sometimes, need to research other ways.
        foreach (var player in UtilitiesX.GetUnfilteredPlayers())
            player.MVPs = 0;
        return HookResult.Stop;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        var gameRules = UtilitiesX.GetGameRules();
        var winner = gameRules?.GetKnifeRoundWinner();
        var team = Match.GetTeamFromCsTeam(winner);
        if (team != null)
        {
            // @todo: Hook TerminateRound and change team there. SFUI_Notice_Target_Saved
            gameRules?.SetRoundEndWinner(team.StartingTeam);
            Match.KnifeRoundWinner = team;
        }
        return HookResult.Continue;
    }
}
