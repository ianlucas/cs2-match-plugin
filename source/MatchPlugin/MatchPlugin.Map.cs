/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public enum MapResult : int
{
    None,
    Completed,
    Cancelled,
    Forfeited
}

public class Map(string mapName)
{
    [JsonPropertyName("mapName")]
    public string MapName = mapName;

    [JsonPropertyName("winner")]
    public int? Winner;

    [JsonPropertyName("result")]
    public MapResult Result = MapResult.None;

    [JsonPropertyName("stats")]
    public object? Stats;

    [JsonPropertyName("demoPath")]
    public string? DemoPath;

    [JsonPropertyName("knifeRoundWinner")]
    public int? KnifeRoundWinner;
}
