/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace MatchPlugin;

public class PlayerStats(ulong steamId)
{
    [JsonPropertyName("steamid")]
    public string SteamID { get; set; } = steamId.ToString();

    [JsonPropertyName("kills")]
    public int Kills { get; set; } = 0;

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; } = 0;

    [JsonPropertyName("assists")]
    public int Assists { get; set; } = 0;

    [JsonPropertyName("flashbang_assists")]
    public int FlashbangAssists { get; set; } = 0;

    [JsonPropertyName("team_kills")]
    public int Teamkills { get; set; } = 0;

    [JsonPropertyName("suicides")]
    public int Suicides { get; set; } = 0;

    [JsonPropertyName("damage")]
    public int Damage { get; set; } = 0;

    [JsonPropertyName("utility_damage")]
    public int UtilDamage { get; set; } = 0;

    [JsonPropertyName("enemies_flashed")]
    public int EnemiesFlashed { get; set; } = 0;

    [JsonPropertyName("friendlies_flashed")]
    public int FriendliesFlashed { get; set; } = 0;

    [JsonPropertyName("knife_kills")]
    public int KnifeKills { get; set; } = 0;

    [JsonPropertyName("headshot_kills")]
    public int HeadshotKills { get; set; } = 0;

    [JsonPropertyName("rounds_played")]
    public int RoundsPlayed { get; set; } = 0;

    [JsonPropertyName("bomb_defuses")]
    public int BombDefuses { get; set; } = 0;

    [JsonPropertyName("bomb_plants")]
    public int BombPlants { get; set; } = 0;

    [JsonPropertyName("1k")]
    public int K1 { get; set; } = 0;

    [JsonPropertyName("2k")]
    public int K2 { get; set; } = 0;

    [JsonPropertyName("3k")]
    public int K3 { get; set; } = 0;

    [JsonPropertyName("4k")]
    public int K4 { get; set; } = 0;

    [JsonPropertyName("5k")]
    public int K5 { get; set; } = 0;

    [JsonPropertyName("1v1")]
    public int V1 { get; set; } = 0;

    [JsonPropertyName("1v2")]
    public int V2 { get; set; } = 0;

    [JsonPropertyName("1v3")]
    public int V3 { get; set; } = 0;

    [JsonPropertyName("1v4")]
    public int V4 { get; set; } = 0;

    [JsonPropertyName("1v5")]
    public int V5 { get; set; } = 0;

    [JsonPropertyName("first_kills_ct")]
    public int FirstKillsCT { get; set; } = 0;

    [JsonPropertyName("first_kills_t")]
    public int FirstKillsT { get; set; } = 0;

    [JsonPropertyName("first_deaths_ct")]
    public int FirstDeathsCT { get; set; } = 0;

    [JsonPropertyName("first_deaths_t")]
    public int FirstDeathsT { get; set; } = 0;

    [JsonPropertyName("trade_kills")]
    public int TradeKills { get; set; } = 0;

    [JsonPropertyName("kast")]
    public int KAST { get; set; } = 0;

    [JsonPropertyName("score")]
    public int Score { get; set; } = 0;

    [JsonPropertyName("mvp")]
    public int MVPs { get; set; } = 0;

    public PlayerStats Clone() => (PlayerStats)MemberwiseClone();
}
