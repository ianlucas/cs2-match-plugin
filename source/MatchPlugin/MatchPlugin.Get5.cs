/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Get5(Match match)
{
    public object OnGameStateChanged(State oldState, State newState) =>
        new
        {
            @event = "game_state_changed",
            new_state = newState.Name,
            old_state = oldState.Name
        };

    public object OnPreLoadMatchConfig(string filename) =>
        new { @event = "preload_match_config", filename };

    public object OnLoadMatchConfigFailed(string reason) =>
        new { @event = "match_config_load_fail", reason };

    public object OnSeriesInit() =>
        new
        {
            @event = "series_start",
            matchid = match.Id,
            num_maps = match.Maps.Count,
            // @todo: team1.id, team2.id
            team1 = new { name = match.Team1.Name },
            team2 = new { name = match.Team2.Name }
        };

    public object OnMapResult(Map map) =>
        new
        {
            @event = "map_result",
            matchid = match.Id,
            map_number = match.FindMapIndex(map),
            team1 = ToGet5StatsTeam(match.Team1),
            team2 = ToGet5StatsTeam(match.Team2),
            // @todo winner may be null
            winner = ToGet5Winner(map.Winner),
            // @todo extended property
            result = map.Result
        };

    public object OnSeriesResult(Team? winner) =>
        new
        {
            @event = "series_end",
            matchid = match.Id,
            team1_series_score = match.Team1.SeriesScore,
            team2_series_score = match.Team2.SeriesScore,
            // @todo winner may be null
            winner = ToGet5Winner(winner),
            time_until_restore = 0
        };

    public object OnSidePicked(Team team)
    {
        var map = match.GetMap();
        return new
        {
            @event = "side_picked",
            matchid = match.Id,
            team = ToGet5TeamString(team),
            map_name = map?.MapName,
            side = ToGet5SideString(team.StartingTeam),
            map_number = match.FindMapIndex(map)
        };
    }

    public object OnBackupRestore(string filename) =>
        new
        {
            @event = "backup_loaded",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            filename
        };

    public object OnDemoFinished(string filename) =>
        new
        {
            @event = "demo_finished",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            filename
        };

    public object OnMatchPaused(Team team, string pauseType) =>
        new
        {
            @event = "game_paused",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType
        };

    public object OnMatchUnpaused(Team? team, string pauseType) =>
        new
        {
            @event = "game_unpaused",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType
        };

    public object OnPauseBegan(Team? team, string pauseType) =>
        new
        {
            @event = "pause_began",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType
        };

    public object OnKnifeRoundStarted() =>
        new
        {
            @event = "knife_start",
            matchid = match.Id,
            map_number = match.GetMapIndex()
        };

    public object OnKnifeRoundWon(Team team, KnifeRoundVote decision) =>
        new
        {
            @event = "knife_won",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            team = ToGet5TeamString(team),
            side = ToGet5SideString(team.StartingTeam),
            swapped = decision == KnifeRoundVote.Switch
        };

    public object OnTeamReadyStatusChanged(Team team) =>
        new
        {
            @event = "team_ready_status_changed",
            matchid = match.Id,
            team = ToGet5TeamString(team),
            ready = team.Players.All(p => p.IsReady),
            game_state = match.State.Name
        };

    public object OnGoingLive() =>
        new
        {
            @event = "going_live",
            matchid = match.Id,
            map_number = match.GetMapIndex()
        };

    public object OnRoundStart() =>
        new
        {
            @event = "round_start",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber()
        };

    public object OnRoundEnd(Team winner, int reason) =>
        new
        {
            @event = "round_start",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            round_time = match.GetRoundTime(),
            reason,
            winner = ToGet5Winner(winner),
            team1 = ToGet5StatsTeam(match.Team1),
            team2 = ToGet5StatsTeam(match.Team2)
        };

    public object OnRoundStatsUpdated() =>
        new
        {
            @event = "round_start",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber()
        };

    public object OnPlayerBecameMVP(Player player, int reason) =>
        new
        {
            @event = "round_mvp",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            player = ToGet5Player(player),
            reason
        };

    public object OnGrenadeThrown(Player player, string weapon) =>
        new
        {
            @event = "grenade_thrown",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            round_time = match.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon)
        };

    public object OnPlayerDeath(
        Player player,
        Player? attacker,
        Player? assister,
        string weapon,
        bool isKilledByBomb,
        bool isHeadshot,
        bool isThruSmoke,
        int isPenetrated,
        bool isAttackerBlind,
        bool isNoScope,
        bool isSuicide,
        bool isFriendlyFire,
        bool isFlashAssist
    ) =>
        new
        {
            @event = "player_death",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            round_time = match.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            bomb = isKilledByBomb,
            headshot = isHeadshot,
            thru_smoke = isThruSmoke,
            penetrated = isPenetrated,
            attacker_blind = isAttackerBlind,
            no_scope = isNoScope,
            suicide = isSuicide,
            friendly_fire = isFriendlyFire,
            attacker,
            assist = assister != null
                ? new
                {
                    player = ToGet5Player(assister),
                    friendly_fire = player.Team == assister.Team,
                    flash_assist = isFlashAssist
                }
                : null
        };

    public object OnHEGrenadeDetonated(Player player, string weapon, UtilityVictims victims) =>
        new
        {
            @event = "hegrenade_detonated",
            matchid = match.Id,
            map_number = match.GetMapIndex(),
            round_number = match.GetRoundNumber(),
            round_time = match.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            victims = victims.Values.Select(victim => new
            {
                player = ToGet5Player(victim.Player),
                killed = victim.Killed,
                damage = victim.Damage
            }),
            damage_enemies = victims
                .Values.Where(v => v.Player.Team != player.Team)
                .Select(v => v.Damage)
                .Sum(),
            damage_friendlies = victims
                .Values.Where(v => v.Player.Team == player.Team)
                .Select(v => v.Damage)
                .Sum()
        };

    private string ToGet5SideString(CsTeam team) => team == CsTeam.Terrorist ? "t" : "ct";

    private string ToGet5TeamString(Team? team) => team != null ? $"team{team.Index + 1}" : "spec";

    private object ToGet5StatsTeam(Team team) =>
        new
        {
            // @todo team.id
            name = team.Name,
            series_score = team.SeriesScore,
            score = team.Score,
            score_ct = team.Stats.ScoreCT,
            score_t = team.Stats.ScoreT,
            side = ToGet5SideString(team.CurrentTeam),
            starting_side = ToGet5SideString(team.StartingTeam),
            players = team
                .Players.Select(player => new
                {
                    steamid = player.SteamID,
                    name = player.Name,
                    stats = player.Stats
                })
                .ToList()
        };

    private object? ToGet5Winner(Team? team) =>
        team != null
            ? new { side = ToGet5SideString(team.CurrentTeam), team = ToGet5TeamString(team) }
            : null;

    private object ToGet5Player(Player player) =>
        new
        {
            steamid = player.SteamID,
            name = player.Name,
            user_id = player.GetIndex(),
            side = ToGet5SideString(player.Team.CurrentTeam),
            is_bot = player.Controller?.IsBot ?? false
        };

    private object ToGet5Weapon(string weapon) =>
        new { name = weapon.Replace("weapon_", ""), id = ItemUtilities.GetItemDefIndex(weapon) };
}
