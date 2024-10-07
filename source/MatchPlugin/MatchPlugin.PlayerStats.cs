/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace MatchPlugin;

public class PlayerStats
{
    [JsonPropertyName("kills")]
    public int Kills = 0;

    [JsonPropertyName("deaths")]
    public int Deaths = 0;

    [JsonPropertyName("assists")]
    public int Assists = 0;

    [JsonPropertyName("flashbangAssists")]
    public int FlashbangAssists = 0;

    [JsonPropertyName("teamKills")]
    public int Teamkills = 0;

    [JsonPropertyName("suicides")]
    public int Suicides = 0;

    [JsonPropertyName("damage")]
    public int Damage = 0;

    [JsonPropertyName("utilDamage")]
    public int UtilDamage = 0;

    [JsonPropertyName("enemiesFlashed")]
    public int EnemiesFlashed = 0;

    [JsonPropertyName("friendliesFlashed")]
    public int FriendliesFlashed = 0;

    [JsonPropertyName("knifeKills")]
    public int KnifeKills = 0;

    [JsonPropertyName("headshotKills")]
    public int HeadshotKills = 0;

    [JsonPropertyName("roundsPlayed")]
    public int RoundsPlayed = 0;

    [JsonPropertyName("bombDefuses")]
    public int BombDefuses = 0;

    [JsonPropertyName("bombPlants")]
    public int BombPlants = 0;

    [JsonPropertyName("k1")]
    public int K1 = 0;

    [JsonPropertyName("k2")]
    public int K2 = 0;

    [JsonPropertyName("k3")]
    public int K3 = 0;

    [JsonPropertyName("k4")]
    public int K4 = 0;

    [JsonPropertyName("k5")]
    public int K5 = 0;

    [JsonPropertyName("v1")]
    public int V1 = 0;

    [JsonPropertyName("v2")]
    public int V2 = 0;

    [JsonPropertyName("v3")]
    public int V3 = 0;

    [JsonPropertyName("v4")]
    public int V4 = 0;

    [JsonPropertyName("v5")]
    public int V5 = 0;

    [JsonPropertyName("firstKillsCT")]
    public int FirstKillsCT = 0;

    [JsonPropertyName("firstKillsT")]
    public int FirstKillsT = 0;

    [JsonPropertyName("firstDeathsCT")]
    public int FirstDeathsCT = 0;

    [JsonPropertyName("firstDeathsT")]
    public int FirstDeathsT = 0;

    [JsonPropertyName("tradeKills")]
    public int TradeKills = 0;

    [JsonPropertyName("kast")]
    public int KAST = 0;

    [JsonPropertyName("score")]
    public int Score = 0;

    [JsonPropertyName("mvps")]
    public int MVPs = 0;
}
