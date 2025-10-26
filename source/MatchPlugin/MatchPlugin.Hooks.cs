/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class MatchPlugin
{
    public HookResult OnChangeTeam(DynamicHook h)
    {
        var controller = h.GetParam<CCSPlayerController>(0);
        if (!controller.IsBot)
        {
            var originalTeam = h.GetParam<int>(1);
            var player = _match.GetPlayerFromSteamID(controller.SteamID);
            if (_match.AreTeamsLocked())
            {
                if (player != null)
                {
                    var currentTeam = (int)player.Team.CurrentTeam;
                    if (originalTeam != currentTeam)
                    {
                        h.SetParam(1, currentTeam);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    var newTeam = (int)CsTeam.Spectator;
                    h.SetParam(1, newTeam);
                    return HookResult.Continue;
                }
            }
        }
        return HookResult.Continue;
    }

    private bool _rememberBotKick = false;

    public HookResult OnMaintainBotQuota(DynamicHook h)
    {
        if (!_match.bots.Value)
        {
            if (_rememberBotKick)
                return HookResult.Stop;
            foreach (var controller in UtilitiesX.GetAllPlayersInTeams())
                if (controller.IsBot)
                    Server.ExecuteCommand($"kickid {controller.UserId}");
            _rememberBotKick = true;
            return HookResult.Stop;
        }
        var neededPerTeam = _match.players_needed_per_team.Value;
        List<(IEnumerable<CCSPlayerController>, string)> teams =
        [
            (UtilitiesX.GetPlayersFromTeam(CsTeam.Terrorist), "t"),
            (UtilitiesX.GetPlayersFromTeam(CsTeam.CounterTerrorist), "ct")
        ];
        foreach (var (team, side) in teams)
        {
            int botCount = 0;
            int humanCount = 0;
            int? botToKick = null;
            foreach (var controller in team)
            {
                if (controller.IsBot == true)
                {
                    botCount++;
                    if (botToKick == null && controller.UserId != null)
                        botToKick = controller.UserId;
                }
                else
                    humanCount++;
            }
            if (botCount + humanCount > neededPerTeam && botToKick != null)
                Server.ExecuteCommand($"kickid {botToKick}");
            if (botCount + humanCount < neededPerTeam)
                Server.ExecuteCommand($"bot_add_{side}");
        }
        return HookResult.Stop;
    }
}
