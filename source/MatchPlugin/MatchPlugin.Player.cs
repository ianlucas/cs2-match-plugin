/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public class Player(ulong steamId, string name, Team team, CCSPlayerController? controller = null)
{
    public bool IsReady = false;
    public CCSPlayerController? Controller = controller;
    public Dictionary<ulong, DamageReport> DamageReport = [];
    public string Name = name;
    public Team Team = team;
    public ulong SteamID = steamId;
}
