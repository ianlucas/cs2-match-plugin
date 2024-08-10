/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Team(Match match, CsTeam startingTeam)
{
    private readonly Match _match = match;
    private Team? _opposition;

    public readonly List<Player> Players = [];
    public readonly List<ulong> SurrenderVotes = [];
    public readonly int Index = (byte)startingTeam - 2;
    public CsTeam StartingTeam = startingTeam;
    public Player? InGameLeader = null;
    public string Name = "";
    public bool IsUnpauseMatch = false;
    public bool IsSurrended = false;
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

    public void Reset()
    {
        Players.Clear();
        SurrenderVotes.Clear();
        InGameLeader = null;
        Name = "";
        IsUnpauseMatch = false;
        IsSurrended = false;
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

    public CsTeam GetCurrentTeam()
    {
        return _match.AreTeamsPlayingSwitchedSides()
            ? UtilitiesX.ToggleCsTeam(StartingTeam)
            : StartingTeam;
    }

    public string GetServerName() =>
        Name == ""
            ? InGameLeader != null
                ? $"team_{InGameLeader.Name}"
                : "\"\""
            : Name;

    public string GetName() =>
        Name == ""
            ? InGameLeader != null
                ? $"team_{InGameLeader.Name}"
                : _match.Plugin.Localizer[
                    GetCurrentTeam() == CsTeam.Terrorist ? "match.t" : "match.ct"
                ]
            : Name;

    public CCSTeam? GetEntity()
    {
        return Utilities
            .FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager")
            .Where(team => team.TeamNum == (byte)GetCurrentTeam())
            .FirstOrDefault();
    }

    public int GetScore()
    {
        return GetEntity()?.Score ?? 0;
    }

    public void PrintToChat(string message)
    {
        foreach (var player in Players)
            player.Controller?.PrintToChat(message);
    }
}
