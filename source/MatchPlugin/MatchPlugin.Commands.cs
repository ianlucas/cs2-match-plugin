/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class MatchPlugin
{
    public void OnMatchStatusCommand(CCSPlayerController? caller, CommandInfo _)
    {
        if (caller != null && !AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        var message = "[MatchPlugin Status]\n\n";
        message += $"State: {_match.State.GetType().Name}\n";
        message += $"Id: {_match.Id ?? "(No ID)"}\n";
        message += $"Loaded from file?: {_match.IsLoadedFromFile}\n";
        message += $"Is matchmaking?: {_match.IsMatchmaking()}\n";
        message += "\n";
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
        caller?.PrintToConsole(message);
        Server.PrintToConsole(message);
    }

    public void OnStartCommand(CCSPlayerController? caller, CommandInfo _)
    {
        if (!AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        if (_match.State is not StateWarmupReady)
            return;
        if (!_match.IsLoadedFromFile)
            foreach (var controller in UtilitiesX.GetPlayersInTeams())
            {
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
        _match.Log(
            printToChat: true,
            message: Localizer[
                "match.admin_start",
                _match.GetChatPrefix(true),
                UtilitiesX.GetPlayerName(caller)
            ]
        );
        _match.Setup();
        _match.SetState(new StateKnifeRound());
    }

    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        if (command.ArgCount != 2)
            return;
        var mapname = command.ArgByIndex(1).ToLower().Trim();
        if (!mapname.StartsWith("de_"))
            return;
        if (_match.AreTeamsLocked())
            return;
        _match.Log(
            printToChat: true,
            message: Localizer[
                "match.admin_map",
                _match.GetChatPrefix(true),
                UtilitiesX.GetPlayerName(caller)
            ]
        );
        Server.ExecuteCommand($"changelevel {mapname}");
    }

    public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        _match.Log(
            printToChat: true,
            message: Localizer[
                "match.admin_restart",
                _match.GetChatPrefix(true),
                UtilitiesX.GetPlayerName(caller)
            ]
        );
        _match.Reset();
        _match.SetState(new StateWarmupReady());
    }

    public void OnMatchLoadCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(caller, "@css/config"))
            return;
        if (_match.State is not StateWarmupReady)
            return;
        if (command.ArgCount != 2)
            return;
        var name = command.ArgByIndex(1).Trim();
        var matchSchema = MatchFile.Read(name, _match);
        if (matchSchema == null)
            return;
        var terrorists = _match.Teams.FirstOrDefault();
        var cts = _match.Teams.LastOrDefault();
        if (terrorists == null || cts == null)
            return;
        _match.Reset();
        _match.IsLoadedFromFile = true;
        _match.Id = matchSchema.MatchId;
        foreach (var mapName in matchSchema.Maps)
            _match.Maps.Add(new(mapName));
        terrorists.StartingTeam = CsTeam.Terrorist;
        cts.StartingTeam = CsTeam.CounterTerrorist;
        for (var index = 0; index < _match.Teams.Count; index++)
        {
            var team = _match.Teams[index];
            var teamSchema = matchSchema.Teams[index];
            team.Name = teamSchema.Name ?? "";
            foreach (var playerSchema in teamSchema.Players)
            {
                var steamId = ulong.Parse(playerSchema.SteamID);
                var player = new Player(
                    steamId,
                    playerSchema.Name,
                    team,
                    Utilities.GetPlayerFromSteamId(steamId)
                );
                team.AddPlayer(player);
                if (playerSchema.IsInGameLeader == true)
                    team.InGameLeader = player;
            }
        }
        if (matchSchema.IsMatchmaking != null)
            _match.matchmaking.Value = matchSchema.IsMatchmaking.Value;
        if (matchSchema.IsTvRecord != null)
            _match.tv_record.Value = matchSchema.IsTvRecord.Value;
        matchSchema.Commands?.ForEach(Server.ExecuteCommand);
        foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
            if (_match.GetPlayerFromSteamID(controller.SteamID) == null)
                if (
                    _match.matchmaking.Value
                    || AdminManager.PlayerHasPermissions(controller, "@css/config")
                )
                    controller.ChangeTeam(CsTeam.Spectator);
                else
                    controller.Kick();
        _match.CreateMatchFolder();
        _match.SetState(new StateWarmupReady());
        _match.SendEvent(_match.Get5.OnSeriesInit());
    }
}
