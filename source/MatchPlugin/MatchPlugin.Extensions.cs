/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public static class Extensions
{
    public static MemoryFunctionVoid<IntPtr, float, RoundEndReason, int, uint> TerminateRoundFunc =
        new(GameData.GetSignature("TerminateRound"));

    public static Action<IntPtr, float, RoundEndReason, int, uint> TerminateRound =
        TerminateRoundFunc.Invoke;

    public static readonly MemoryFunctionVoid<IntPtr, int> ChangeTeamFunc =
        new(GameData.GetSignature("ChangeTeam"));

    public static readonly Action<IntPtr, int> ChangeTeam = ChangeTeamFunc.Invoke;

    public static void SetClan(this CCSPlayerController controller, string clan)
    {
        try
        {
            if (controller.Clan != clan)
            {
                controller.Clan = clan;
                Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
                new GameEvent("nextlevel_changed", false).FireEvent(false);
            }
        }
        catch { }
    }

    public static int GetHealth(this CCSPlayerController controller)
    {
        return Math.Max(
            (
                controller.IsBot == true ? controller.Pawn?.Value : controller.PlayerPawn?.Value
            )?.Health ?? 0,
            0
        );
    }

    public static void Kick(this CCSPlayerController controller)
    {
        if (controller.UserId.HasValue)
        {
            Server.ExecuteCommand($"kickid {(ushort)controller.UserId}");
        }
    }

    public static void TerminateRoundX(
        this CCSGameRules gameRules,
        float delay,
        RoundEndReason roundEndReason
    ) => TerminateRound(gameRules.Handle, delay, roundEndReason, 0, 0);

    public static CsTeam GetKnifeRoundWinner(this CCSGameRules _)
    {
        var tPlayers = UtilitiesX.GetPlayersFromTeam(CsTeam.Terrorist);
        var ctPlayers = UtilitiesX.GetPlayersFromTeam(CsTeam.CounterTerrorist);
        int tAlive = tPlayers.Count(CountAlive);
        int tHealth = tPlayers.Sum(SumHealth);
        int ctAlive = ctPlayers.Count(CountAlive);
        int ctHealth = ctPlayers.Sum(SumHealth);
        if (ctAlive != tAlive)
        {
            var winner = ctAlive > tAlive ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            Server.PrintToConsole(
                $"CCSGameRules::GetCustomRoundWinner (Alive ct={ctAlive} t={tAlive}) winner={winner}"
            );
            return winner;
        }
        if (ctHealth != tHealth)
        {
            var winner = ctHealth > tHealth ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            Server.PrintToConsole(
                $"CCSGameRules::GetCustomRoundWinner (Health ct={ctHealth} t={tHealth}) winner={winner}"
            );
            return winner;
        }
        var randomWinner = (CsTeam)new Random().Next(2, 4);
        Server.PrintToConsole(
            $"CCSGameRules::GetCustomRoundWinner (Random) randomWinner={randomWinner}"
        );
        return randomWinner;

        static bool CountAlive(CCSPlayerController player) => player.GetHealth() > 0;
        static int SumHealth(CCSPlayerController player) => player.GetHealth();
    }

    public static void SetRoundEndWinner(this CCSGameRules gameRules, CsTeam team)
    {
        var reason = 10;
        var message = "";
        switch (team)
        {
            case CsTeam.CounterTerrorist:
                reason = 8;
                message = "#SFUI_Notice_CTs_Win";
                break;
            case CsTeam.Terrorist:
                reason = 9;
                message = "#SFUI_Notice_Terrorists_Win";
                break;
        }
        gameRules.RoundEndReason = reason;
        gameRules.RoundEndFunFactToken = "";
        gameRules.RoundEndMessage = message;
        gameRules.RoundEndWinnerTeam = (int)team;
        gameRules.RoundEndFunFactData1 = 0;
        gameRules.RoundEndFunFactPlayerSlot = 0;
    }
}
