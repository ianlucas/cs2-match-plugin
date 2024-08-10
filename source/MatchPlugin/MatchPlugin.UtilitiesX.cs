﻿/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class UtilitiesX
{
    static CCSGameRulesProxy? GameRulesProxy;

    public static IEnumerable<CCSPlayerController> GetPlayersFromTeam(CsTeam team) =>
        Utilities.GetPlayers().Where(p => p.Team == team);

    public static IEnumerable<CCSPlayerController> GetPlayersInTeams() =>
        Utilities.GetPlayers().Where(p => p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist);

    public static IEnumerable<CCSPlayerController> GetUnfilteredPlayers() =>
        Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

    public static CCSGameRules? GetGameRules() =>
        GameRulesProxy?.IsValid == true
            ? GameRulesProxy.GameRules
            : (
                GameRulesProxy = Utilities
                    .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                    .First()
            )?.IsValid == true
                ? GameRulesProxy?.GameRules
                : null;
}