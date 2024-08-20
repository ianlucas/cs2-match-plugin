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
    public override void Load()
    {
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Extensions.IncrementNumMVPsFunc.Hook(OnIncrementNumMVPs, HookMode.Pre);
        Extensions.TerminateRoundFunc.Hook(OnTerminateRound, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);

        Config.ExecKnife();

        Match.KnifeRoundWinner = null;
        Match.Cstv.Record(Match.GetDemoFilename());

        foreach (var player in Utilities.GetPlayers())
            player.SetClan("");
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
            Match.SetState<StateWarmupKnifeVote>();
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
        var winner = UtilitiesX.GetGameRules()?.GetKnifeRoundWinner() ?? CsTeam.Terrorist;
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
        var gameRules = UtilitiesX.GetGameRules();
        if (gameRules != null && Match.KnifeRoundWinner == null)
        {
            Match.KnifeRoundWinner = Match.GetTeamFromCsTeam(gameRules.GetKnifeRoundWinner());
        }
        return HookResult.Continue;
    }
}
