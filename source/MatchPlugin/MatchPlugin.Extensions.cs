/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public static partial class Extensions
{
    public static readonly MemoryFunctionVoid<IntPtr, int> ChangeTeamFunc =
        new(GameData.GetSignature("ChangeTeam"));

    public static readonly MemoryFunctionWithReturn<IntPtr, int, int> IncrementNumMVPsFunc =
        new(GameData.GetSignature("IncrementNumMVPs"));

    public static readonly MemoryFunctionVoid<IntPtr> HandleSwapTeamsFunc =
        new(GameData.GetSignature("HandleSwapTeams"));

    public static readonly MemoryFunctionWithReturn<IntPtr, bool> IsLastRoundBeforeHalfTimeFunc =
        new(GameData.GetSignature("IsLastRoundBeforeHalfTime"));

    public static readonly MemoryFunctionWithReturn<IntPtr, bool> AreTeamsPlayingSwitchedSidesFunc =
        new(GameData.GetSignature("AreTeamsPlayingSwitchedSides"));

    public static readonly MemoryFunctionWithReturn<IntPtr, byte> MaintainBotQuotaFunc =
        new(GameData.GetSignature("MaintainBotQuota"));

    public static void HandleSwapTeams(this CCSGameRules gameRules) =>
        HandleSwapTeamsFunc.Invoke(gameRules.Handle);

    public static bool IsLastRoundBeforeHalfTime(this CCSGameRules gameRules) =>
        IsLastRoundBeforeHalfTimeFunc.Invoke(gameRules.Handle);

    public static bool AreTeamsPlayingSwitchedSides(this CCSGameRules gameRules) =>
        AreTeamsPlayingSwitchedSidesFunc.Invoke(gameRules.Handle);

    public static int GetHealth(this CCSPlayerController controller) =>
        Math.Max(
            (
                controller.IsBot == true ? controller.Pawn?.Value : controller.PlayerPawn?.Value
            )?.Health ?? 0,
            0
        );

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

    public static string StripColorTags(this string str) => ColorTag().Replace(str, "");

    public static string StripQuotes(this string str) =>
        str.Length >= 2 && str.StartsWith('"') && str.EndsWith('"') ? str[1..^1] : str;

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex ColorTag();
}
