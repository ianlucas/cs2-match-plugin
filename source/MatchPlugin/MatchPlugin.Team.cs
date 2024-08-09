/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Team(CsTeam startingTeam)
{
    private Team? _opposition;

    public CsTeam StartingTeam = startingTeam;
    public Player? InGameLeader = null;
    public readonly int Index = startingTeam == CsTeam.Terrorist ? 0 : 1;
    public string Name = "";
    public Team Oppositon
    {
        get
        {
            if (_opposition == null)
                throw new ArgumentException("No opposition defined");
            return _opposition;
        }
        set { _opposition = value; }
    }
}
