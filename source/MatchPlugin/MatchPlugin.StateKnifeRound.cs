/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class StateKnifeRound(Match match) : State(match)
{
    private bool _didKnifeVoteStart = false;

    public override void Load()
    {
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Extensions.IncrementNumMVPsFunc.Hook(OnIncrementNumMVPs, HookMode.Pre);
        Extensions.TerminateRoundFunc.Hook(OnTerminateRound, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);

        Match.Log("Execing knife round...");
        Config.ExecKnife();

        Match.KnifeRoundWinner = null;
        Match.Cstv.Record(Match.GetDemoFilename());

        UtilitiesX.RemovePlayerClans();
    }

    public override void Unload()
    {
        Match.Plugin.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Extensions.IncrementNumMVPsFunc.Unhook(OnIncrementNumMVPs, HookMode.Pre);
        Extensions.TerminateRoundFunc.Unhook(OnTerminateRound, HookMode.Pre);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        if (Match.KnifeRoundWinner != null)
        {
            if (!_didKnifeVoteStart)
            {
                _didKnifeVoteStart = true;
                Match.SetState(new StateWarmupKnifeVote(Match));
            }
        }
        else
            ServerX.PrintToChatAllRepeat(
                Match.Plugin.Localizer["match.knife", Match.GetChatPrefix()]
            );
        return HookResult.Continue;
    }

    public HookResult OnIncrementNumMVPs(DynamicHook h) => HookResult.Stop;

    public HookResult OnTerminateRound(DynamicHook h)
    {
        var winner = UtilitiesX.GetGameRules().GetKnifeRoundWinner();
        Match.KnifeRoundWinner = Match.GetTeamFromCsTeam(winner);
        h.SetParam(
            2,
            (uint)(
                winner == CsTeam.Terrorist ? RoundEndReason.TerroristsWin : RoundEndReason.CTsWin
            )
        );
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        Match.KnifeRoundWinner ??= Match.GetTeamFromCsTeam(
            UtilitiesX.GetGameRules().GetKnifeRoundWinner()
        );
        return HookResult.Continue;
    }
}
