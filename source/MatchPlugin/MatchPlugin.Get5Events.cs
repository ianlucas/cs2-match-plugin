/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.IO;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Get5Events
{
    public static object OnGameStateChanged(State oldState, State newState) =>
        new
        {
            @event = "game_state_changed",
            new_state = newState.Name,
            old_state = oldState.Name
        };

    public static object OnPreLoadMatchConfig(string filename) =>
        new { @event = "preload_match_config", filename };

    public static object OnLoadMatchConfigFailed(string reason) =>
        new { @event = "match_config_load_fail", reason };

    public static object OnSeriesInit(Match match)
    {
        var team1 = match.Teams.First();
        var team2 = team1.Oppositon;
        return new
        {
            @event = "series_start",
            matchid = match.Id,
            num_maps = match.Maps.Count,
            // @todo: team1.id, team2.id
            team1 = new { name = team1.Name },
            team2 = new { name = team2.Name }
        };
    }

    public static object OnMapResult(Match match, Map map)
    {
        var team1 = match.Teams.First();
        var team2 = team1.Oppositon;
        var winner =
            map.Winner != null
                ? team1.Index == map.Winner
                    ? team1
                    : team2
                : null;
        return new
        {
            @event = "map_result",
            matchid = match.Id,
            map_number = GetMapNumber(match.Maps.IndexOf(map)),
            team1 = GetStatsTeam(team1),
            team2 = GetStatsTeam(team2),
            // @todo winner may be null
            winner = winner != null ? GetWinner(winner) : null,
            // @todo extended property
            result = map.Result
        };
    }

    public static object OnSeriesResult(Match match, Team? winner)
    {
        var team1 = match.Teams.First();
        var team2 = team1.Oppositon;
        return new
        {
            @event = "series_end",
            matchid = match.Id,
            team1_series_score = team1.SeriesScore,
            team2_series_score = team2.SeriesScore,
            // @todo winner may be null
            winner = winner != null ? GetWinner(winner) : null,
            // We immediatelly go to warmup regardless.
            time_until_restore = 0
        };
    }

    public static object OnSidePicked(Match match, Team team)
    {
        var map = match.GetCurrentMap();
        return new
        {
            @event = "side_picked",
            matchid = match.Id,
            team = GetTeamString(team),
            // This should never be null even tho technically it may be.
            map_name = map?.MapName,
            side = GetCsTeamString(team.StartingTeam),
            map_number = map != null ? GetMapNumber(match.Maps.IndexOf(map)) : 0
        };
    }

    public static object OnBackupRestore(Match match, int round_number, string filename)
    {
        var map = match.GetCurrentMap();
        return new
        {
            @event = "backup_loaded",
            matchid = match.Id,
            map_number = map != null ? GetMapNumber(match.Maps.IndexOf(map)) : 0,
            round_number,
            filename
        };
    }

    public static object OnDemoFinished(Match match, string filename)
    {
        var map = match.GetCurrentMap();
        return new
        {
            @event = "demo_finished",
            matchid = match.Id,
            map_number = map != null ? GetMapNumber(match.Maps.IndexOf(map)) : 0,
            filename
        };
    }

    public static string GetCsTeamString(CsTeam team) => team == CsTeam.Terrorist ? "t" : "ct";

    public static string GetTeamString(Team team) => $"team{team.Index + 1}";

    public static int GetMapNumber(int mapNumber) => mapNumber > -1 ? mapNumber : 0;

    public static object GetStatsTeam(Team team)
    {
        return new
        {
            // @todo team.id
            name = team.Name,
            series_score = team.SeriesScore,
            score = team.Score,
            score_ct = team.Stats.ScoreCT,
            score_t = team.Stats.ScoreT,
            side = GetCsTeamString(team.CurrentTeam),
            starting_side = GetCsTeamString(team.StartingTeam),
            players = team
                .Players.Select(player => new
                {
                    steamid = player.SteamID,
                    name = player.Name,
                    stats = player.Stats
                })
                .ToList()
        };
    }

    public static object GetWinner(Team team)
    {
        return new { side = GetCsTeamString(team.CurrentTeam), team = GetTeamString(team) };
    }
}
