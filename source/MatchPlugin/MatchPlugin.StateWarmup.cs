/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public class StateWarmup : State
{
    public override void Load()
    {
        Match.Plugin.RegisterEventHandler<EventItemPickup>(OnItemPickup);
    }

    public override void Unload()
    {
        Match.Plugin.DeregisterEventHandler<EventItemPickup>(OnItemPickup);
    }

    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        if (@event.Userid != null)
        {
            var inGameMoneyServices = @event.Userid.InGameMoneyServices;
            if (inGameMoneyServices != null)
            {
                inGameMoneyServices.Account = 16000;
                Utilities.SetStateChanged(
                    @event.Userid,
                    "CCSPlayerController",
                    "m_pInGameMoneyServices"
                );
            }
        }
        return HookResult.Continue;
    }
}
