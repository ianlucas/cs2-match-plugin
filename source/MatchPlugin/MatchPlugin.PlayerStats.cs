/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace MatchPlugin;

public class PlayerStats(ulong steamId)
{
    [JsonPropertyName("steamId")]
    public string SteamID { get; set; } = steamId.ToString();

    [JsonPropertyName("kills")]
    public int Kills { get; set; } = 0;

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; } = 0;

    [JsonPropertyName("assists")]
    public int Assists { get; set; } = 0;

    [JsonPropertyName("flashbangAssists")]
    public int FlashbangAssists { get; set; } = 0;

    [JsonPropertyName("teamKills")]
    public int Teamkills { get; set; } = 0;

    [JsonPropertyName("suicides")]
    public int Suicides { get; set; } = 0;

    [JsonPropertyName("damage")]
    public int Damage { get; set; } = 0;

    [JsonPropertyName("utilDamage")]
    public int UtilDamage { get; set; } = 0;

    [JsonPropertyName("enemiesFlashed")]
    public int EnemiesFlashed { get; set; } = 0;

    [JsonPropertyName("friendliesFlashed")]
    public int FriendliesFlashed { get; set; } = 0;

    [JsonPropertyName("knifeKills")]
    public int KnifeKills { get; set; } = 0;

    [JsonPropertyName("headshotKills")]
    public int HeadshotKills { get; set; } = 0;

    [JsonPropertyName("roundsPlayed")]
    public int RoundsPlayed { get; set; } = 0;

    [JsonPropertyName("bombDefuses")]
    public int BombDefuses { get; set; } = 0;

    [JsonPropertyName("bombPlants")]
    public int BombPlants { get; set; } = 0;

    [JsonPropertyName("k1")]
    public int K1 { get; set; } = 0;

    [JsonPropertyName("k2")]
    public int K2 { get; set; } = 0;

    [JsonPropertyName("k3")]
    public int K3 { get; set; } = 0;

    [JsonPropertyName("k4")]
    public int K4 { get; set; } = 0;

    [JsonPropertyName("k5")]
    public int K5 { get; set; } = 0;

    [JsonPropertyName("v1")]
    public int V1 { get; set; } = 0;

    [JsonPropertyName("v2")]
    public int V2 { get; set; } = 0;

    [JsonPropertyName("v3")]
    public int V3 { get; set; } = 0;

    [JsonPropertyName("v4")]
    public int V4 { get; set; } = 0;

    [JsonPropertyName("v5")]
    public int V5 { get; set; } = 0;

    [JsonPropertyName("firstKillsCT")]
    public int FirstKillsCT { get; set; } = 0;

    [JsonPropertyName("firstKillsT")]
    public int FirstKillsT { get; set; } = 0;

    [JsonPropertyName("firstDeathsCT")]
    public int FirstDeathsCT { get; set; } = 0;

    [JsonPropertyName("firstDeathsT")]
    public int FirstDeathsT { get; set; } = 0;

    [JsonPropertyName("tradeKills")]
    public int TradeKills { get; set; } = 0;

    [JsonPropertyName("kast")]
    public int KAST { get; set; } = 0;

    [JsonPropertyName("score")]
    public int Score { get; set; } = 0;

    [JsonPropertyName("mvps")]
    public int MVPs { get; set; } = 0;

    public PlayerStats Clone() => (PlayerStats)MemberwiseClone();
}
