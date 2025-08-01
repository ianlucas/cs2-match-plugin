﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;

namespace MatchPlugin;

public partial class MatchPlugin
{
    private bool _pendingInternalPush = true;
    private readonly ConcurrentDictionary<
        int,
        Action<CCSPlayerController>
    > _pendingOnPlayerConnected = [];

    public void OnMapStart(string _)
    {
        Server.ExecuteCommand("sv_hibernate_when_empty 0");
        _pendingInternalPush = true;
    }

    public void OnTick()
    {
        if (_pendingInternalPush)
        {
            _pendingInternalPush = false;
            OnConfigsExecuted();
        }
        if (_match.bots.Value)
            OnBotsTick();
        if (_match.AreTeamsLocked())
            OnTeamsLocked();
    }

    public void OnConfigsExecuted()
    {
        OnMatchBotsChanged(null, _match.bots.Value);
        OnMatchMatchmakingChanged(null, _match.matchmaking.Value);
        _match.SetState(_match.IsSeriesStarted ? new StateWarmupReady() : new StateNone());
    }

    public void OnClientConnect(int slot, string name, string ipAddress)
    {
        // It may be too early to get controller from the slot.
        _pendingOnPlayerConnected.TryAdd(
            slot,
            (CCSPlayerController controller) =>
                _match.SendEvent(_match.Get5.OnPlayerConnected(controller, ipAddress))
        );
    }

    public void OnClientDisconnect(int slot)
    {
        _pendingOnPlayerConnected.TryRemove(slot, out var _);
    }

    public void OnMatchBotsChanged(object? _, bool value)
    {
        ServerX.ExecuteCommand(
            ["bot_quota_mode fill", $"bot_quota {(value ? _match.players_needed.Value : 0)}"]
        );
    }

    public void OnMatchMatchmakingChanged(object? _, bool value)
    {
        if (value)
            foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
                if (_match.GetPlayerFromSteamID(controller.SteamID) == null)
                    if (AdminManager.PlayerHasPermissions(controller, "@css/root"))
                        controller.ChangeTeam(CsTeam.Spectator);
                    else
                        controller.Disconnect(
                            NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY
                        );
    }

    public void OnBotsTick()
    {
        var neededPerTeam = _match.players_needed_per_team.Value;
        List<IEnumerable<CCSPlayerController>> teams =
        [
            UtilitiesX.GetPlayersFromTeam(CsTeam.Terrorist),
            UtilitiesX.GetPlayersFromTeam(CsTeam.CounterTerrorist)
        ];
        foreach (var team in teams)
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
        }
    }

    public void OnTeamsLocked()
    {
        foreach (var player in _match.Teams.SelectMany(t => t.Players))
            UtilitiesX.SetPlayerName(player.Controller, player.Name);
    }
}
