/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public static class Extensions
{
    public static void SetClan(this CCSPlayerController controller, string clan)
    {
        try
        {
            if (controller.Clan != clan)
            {
                controller.Clan = clan;
                Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
                new GameEvent("nextlevel_changed", false).FireEvent(false);
            }
        }
        catch { }
    }

    public static int GetHealth(this CCSPlayerController controller)
    {
        return Math.Max(
            (
                controller.IsBot == true ? controller.Pawn?.Value : controller.PlayerPawn?.Value
            )?.Health ?? 0,
            0
        );
    }

    public static void Kick(this CCSPlayerController controller)
    {
        if (controller.UserId.HasValue)
        {
            Server.ExecuteCommand($"kickid {(ushort)controller.UserId}");
        }
    }
}
