/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class MatchData
{
    [JsonPropertyName("matchId")]
    public string? MatchId { get; set; }

    [JsonPropertyName("eventsUrl")]
    public string? EventsUrl { get; set; }

    [JsonPropertyName("isMatchmaking")]
    public bool? IsMatchmaking { get; set; }

    [JsonPropertyName("isTvRecord")]
    public bool? IsTvRecord { get; set; }

    [JsonPropertyName("teams")]
    public required List<MatchTeamData> Teams { get; set; }

    [JsonPropertyName("maps")]
    public required List<string> Maps { get; set; }

    [JsonPropertyName("commands")]
    public List<string>? Commands { get; set; }
}

public class MatchTeamData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("players")]
    public required List<MatchPlayerData> Players { get; set; }
}

public class MatchPlayerData
{
    [JsonPropertyName("steamId")]
    public required string SteamID { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("isInGameLeader")]
    public required bool IsInGameLeader { get; set; }
}

public class MatchFile
{
    public static MatchData? Read(string name)
    {
        try
        {
            var path = ServerX.GetFullPath($"/{name}.json");
            var matchString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MatchData>(matchString);
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"Error reading match file: {ex.Message}");
            return null;
        }
    }
}
