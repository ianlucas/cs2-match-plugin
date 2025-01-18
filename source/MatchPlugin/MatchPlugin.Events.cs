/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace MatchPlugin;

public partial class MatchPlugin
{
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid, @event.Address);
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid);
        return HookResult.Continue;
    }

    public void OnPlayerConnected(CCSPlayerController? controller, string? ipAddress = null)
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

        if (ipAddress != null)
        {
            if (player != null)
                _match.SendEvent(_match.Get5.OnPlayerConnected(player, ipAddress));
            else if (controller != null)
                _match.SendEvent(_match.Get5.OnPlayerConnected(controller, ipAddress));
        }
    }

    public HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo _)
    {
        var message = @event.Text.Trim();
        if (message.Length == 0)
            return HookResult.Continue;
        var controller = Utilities.GetPlayerFromUserid(@event.Userid);
        if (controller != null)
            _match.SendEvent(
                _match.Get5.OnPlayerSay(
                    player: controller,
                    @event.Teamonly ? "say_team" : "team",
                    message
                )
            );
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var controller = @event.Userid;
        if (controller != null)
        {
            var player = _match.GetPlayerFromSteamID(controller.SteamID);
            if (player != null)
            {
                player.Controller = null;
                _match.SendEvent(_match.Get5.OnPlayerDisconnected(player));
            }
            else
                _match.SendEvent(_match.Get5.OnPlayerDisconnected(controller));
        }
        return HookResult.Continue;
    }
}
