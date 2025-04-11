/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

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
    public string MapName { get; set; } = mapName;

    [JsonIgnore]
    public Team? Winner { get; set; }

    [JsonPropertyName("winner")]
    public int? WinnerIndex => Winner?.Index;

    [JsonPropertyName("scores")]
    public List<int> Scores { get; set; } = [];

    [JsonPropertyName("result")]
    public MapResult Result { get; set; } = MapResult.None;

    [JsonPropertyName("stats")]
    public List<List<PlayerStats>> Stats { get; set; } = [];

    [JsonPropertyName("demoFilename")]
    public string? DemoFilename { get; set; }

    [JsonPropertyName("knifeRoundWinner")]
    public int? KnifeRoundWinner { get; set; }
}

public class MapEndResult
{
    public required Map Map { get; set; }
    public required bool IsSeriesOver { get; set; }
    public Team? Winner { get; set; }
}
