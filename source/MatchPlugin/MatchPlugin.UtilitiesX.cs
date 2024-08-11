/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Xml.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class UtilitiesX
{
    static CCSGameRulesProxy? GameRulesProxy;

    public static string FormatTimeString(long seconds) => $"{seconds / 60}:{seconds % 60:D2}";

    public static IEnumerable<CCSPlayerController> GetPlayersFromTeam(CsTeam team) =>
        Utilities.GetPlayers().Where(p => p.Team == team);

    public static IEnumerable<CCSPlayerController> GetPlayersInTeams() =>
        Utilities.GetPlayers().Where(p => p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist);

    public static IEnumerable<CCSPlayerController> GetUnfilteredPlayers() =>
        Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

    public static CCSTeam? GetTeamManager(CsTeam team) =>
        Utilities
            .FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager")
            .Where(t => t.TeamNum == (byte)team)
            .FirstOrDefault();

    public static CsTeam ToggleCsTeam(CsTeam team) =>
        team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

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

    public static void SetPlayerName(CCSPlayerController? controller, string name)
    {
        try
        {
            if (controller != null && controller.PlayerName != name)
            {
                controller.PlayerName = name;
                new GameEvent("nextlevel_changed", false).FireEvent(false);
            }
        }
        catch { }
    }
}
