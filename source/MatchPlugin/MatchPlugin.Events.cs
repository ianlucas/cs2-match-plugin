﻿/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace MatchPlugin;

public partial class MatchPlugin
{
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid);
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid);
        return HookResult.Continue;
    }

    public void OnPlayerConnected(CCSPlayerController? controller)
    {
        var player = _match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null)
            player.Controller = controller;
        else if (
            controller?.IsBot == false
            && _match.matchmaking.Value
            && !AdminManager.PlayerHasPermissions(controller, "@css/root")
        )
        {
            controller.Kick();
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = _match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
            player.Controller = null;
        return HookResult.Continue;
    }
}
