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
        if (controller == null)
            return;

        if (_pendingOnPlayerConnected.TryRemove(controller.Slot, out var sendPlayerConnected))
            sendPlayerConnected(controller);

        var player = _match.GetPlayerFromSteamID(controller.SteamID);
        if (player != null)
        {
            if (player.Name == "")
                player.Name = controller.PlayerName;
            player.Controller = controller;
        }
        else if (
            !controller.IsBot
            && _match.matchmaking.Value
            && _match.matchmaking_kick.Value
            && !AdminManager.PlayerHasPermissions(controller, "@css/root")
        )
            controller.Kick();
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
                player.Controller = null;

            _match.SendEvent(_match.Get5.OnPlayerDisconnected(controller));
        }
        return HookResult.Continue;
    }
}
