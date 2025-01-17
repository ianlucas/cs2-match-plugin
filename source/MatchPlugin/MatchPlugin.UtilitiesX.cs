/*---------------------------------------------------------------------------------------------
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

    public static string FormatTimeString(long seconds) => $"{seconds / 60}:{seconds % 60:D2}";

    public static IEnumerable<CCSPlayerController> GetPlayersFromTeam(CsTeam team) =>
        Utilities.GetPlayers().Where(p => p.Team == team);

    public static IEnumerable<CCSPlayerController> GetPlayersInTeams() =>
        Utilities
            .GetPlayers()
            .Where(p => p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist && !p.IsBot);

    public static IEnumerable<CCSPlayerController> GetUnfilteredPlayers() =>
        Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

    public static CCSTeam? GetTeamManager(CsTeam team) =>
        Utilities
            .FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager")
            .Where(t => t.TeamNum == (byte)team)
            .FirstOrDefault();

    public static CsTeam ToggleCsTeam(CsTeam team) =>
        team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

    public static CCSGameRules GetGameRules() =>
        (
            GameRulesProxy?.IsValid == true
                ? GameRulesProxy.GameRules
                : (
                    GameRulesProxy = Utilities
                        .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                        .First()
                )?.IsValid == true
                    ? GameRulesProxy?.GameRules
                    : null
        ) ?? throw new Exception("Game rules not found.");

    public static bool SetPlayerName(CCSPlayerController? controller, string name)
    {
        if (controller != null && controller.PlayerName != name)
        {
            controller.PlayerName = name;
            Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
            return true;
        }
        return false;
    }

    public static bool SetPlayerClan(CCSPlayerController? controller, string clan)
    {
        if (controller != null && controller.Clan != clan)
        {
            controller.Clan = clan;
            Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
            return true;
        }
        return false;
    }

    public static string GetPlayerName(CCSPlayerController? controller) =>
        controller?.PlayerName ?? "Console";

    public static void RemovePlayerClans()
    {
        bool didUpdateControllers = false;
        foreach (var player in Utilities.GetPlayers())
            if (SetPlayerClan(player, ""))
                didUpdateControllers = true;
        if (didUpdateControllers)
            ServerX.UpdatePlayersScoreboard();
    }

    public static IEnumerable<CCSPlayerController> GetAlivePlayersInTeam(CsTeam team) =>
        GetPlayersFromTeam(team).Where(player => player.GetHealth() > 0);

    public static int CountAlivePlayersInTeam(CsTeam team) => GetAlivePlayersInTeam(team).Count();
}
