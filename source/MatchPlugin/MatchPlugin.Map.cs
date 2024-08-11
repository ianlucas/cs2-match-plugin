/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

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
    public string MapName = mapName;
    public int? Winner;
    public MapResult Result = MapResult.None;
    public object? Stats = null;
}
