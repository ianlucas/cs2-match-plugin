/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace MatchPlugin;

public class TeamStats
{
    [JsonPropertyName("score_ct")]
    public int ScoreCT { get; set; } = 0;

    [JsonPropertyName("score_t")]
    public int ScoreT { get; set; } = 0;
}
