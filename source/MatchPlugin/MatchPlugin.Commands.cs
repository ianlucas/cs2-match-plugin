/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class MatchPlugin
{
    public void OnMatchStatusCommand(CCSPlayerController? controller, CommandInfo _)
    {
        if (controller != null && !AdminManager.PlayerHasPermissions(controller, "@css/config"))
            return;
        var message = "[MatchPlugin Status]\n\n";
        message += "[State]\n";
        message += _match.State.GetType().Name;
        message += "\n\n";
        foreach (var team in _match.Teams)
        {
            message += $"[Team {team.Index}]\n";
            if (team.Players.Count == 0)
                message += "No players.\n";
            foreach (var player in team.Players)
            {
                message += $"{player.Name}";
                if (team.InGameLeader == player)
                    message += "[L]";
                if (player.Controller != null)
                {
                    var playerTeam = (player.Controller.Team) switch
                    {
                        CsTeam.Terrorist => "Terrorist",
                        CsTeam.CounterTerrorist => "CT",
                        CsTeam.Spectator => "Spectator",
                        _ => $"Other={player.Controller.Team}"
                    };
                    message += $" ({playerTeam})";
                }
                else
                    message += " (Disconnected)";
                message += "\n";
            }
        }
        if (controller != null)
            controller.PrintToConsole(message);
        Server.PrintToConsole(message);
    }

    public void OnStartCommand(CCSPlayerController? caller, CommandInfo _)
    {
        if (caller != null && !AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        foreach (var controller in UtilitiesX.GetPlayersInTeams())
        {
            controller.SetClan("");
            var player = _match.GetPlayerFromSteamID(controller.SteamID);
            if (player == null)
            {
                var team = _match.GetTeamFromCsTeam(controller.Team);
                if (team == null)
                    controller.ChangeTeam(CsTeam.Spectator);
                else
                {
                    player = new(controller.SteamID, controller.PlayerName, team, controller);
                    team.AddPlayer(player);
                }
            }
            if (player != null)
                player.IsReady = true;
        }
        _match.SetState<StateKnifeRound>();
    }
}
