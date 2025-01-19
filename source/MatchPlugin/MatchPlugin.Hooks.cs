/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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
                        h.SetParam<int>(1, currentTeam);
                        return HookResult.Continue;
                    }
                }
                else
                {
                    var newTeam = (int)CsTeam.Spectator;
                    h.SetParam<int>(1, newTeam);
                    return HookResult.Continue;
                }
            }
        }
        return HookResult.Continue;
    }
}
