﻿/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive
{
    public void OnPauseCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null)
        {
            if (Match.friendly_pause.Value)
            {
                foreach (var team in Match.Teams)
                    team.IsUnpauseMatch = false;
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.pause_start",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
                Server.ExecuteCommand("mp_pause_match");
                return;
            }
            var currentTeam = player.Team.CurrentTeam;
            var gameRules = UtilitiesX.GetGameRules();
            var timeouts =
                currentTeam == CsTeam.Terrorist
                    ? gameRules?.TerroristTimeOuts
                    : gameRules?.CTTimeOuts;
            var timeoutActive =
                currentTeam == CsTeam.Terrorist
                    ? gameRules?.TerroristTimeOutActive
                    : gameRules?.CTTimeOutActive;
            if (timeouts != null && timeouts > 0 && timeoutActive == false)
            {
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.pause_start",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
                Server.ExecuteCommand(
                    currentTeam == CsTeam.Terrorist
                    && (gameRules?.FreezePeriod == true || !Match.AreTeamsSwitchingSidesNextRound())
                        ? "timeout_terrorist_start"
                        : "timeout_ct_start"
                );
            }
        }
    }

    public void OnUnpauseCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (
            player != null
            && (
                Match.friendly_pause.Value
                || AdminManager.PlayerHasPermissions(player.Controller, "@css/config")
            )
        )
        {
            player.Team.IsUnpauseMatch = true;
            if (!Match.Teams.All(team => team.IsUnpauseMatch))
            {
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.pause_unpause1",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
                return;
            }
            else
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.pause_unpause2",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
            Server.ExecuteCommand("mp_unpause_match");
        }
    }
}