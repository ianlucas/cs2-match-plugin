/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Team(Match match, CsTeam startingTeam)
{
    private Match _match = match;
    private Team? _opposition;

    public List<Player> Players = [];
    public CsTeam StartingTeam = startingTeam;
    public Player? InGameLeader = null;
    public readonly int Index = (byte)startingTeam - 2;
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

    public bool CanAddPlayer()
    {
        return Players.Count < _match.players_needed_per_team.Value;
    }

    public void AddPlayer(Player player)
    {
        Players.Add(player);
        InGameLeader ??= player;
    }

    public void RemovePlayer(Player player)
    {
        Players.Remove(player);
        if (InGameLeader == player)
            InGameLeader = Players.FirstOrDefault();
    }
}
