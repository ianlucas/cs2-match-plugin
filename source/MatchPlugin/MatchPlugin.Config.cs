/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class Config
{
    public static void ExecWarmup(int warmupTime = -1, bool lockTeams = false) =>
        ServerX.ExecuteCommand(
            [
                "bot_chatter off",
                "bot_join_after_player 0",
                "mp_autokick 0",
                "mp_autoteambalance 0",
                "mp_buy_allow_grenades 0",
                "mp_ct_default_secondary weapon_hkp2000",
                "mp_death_drop_gun 0",
                "mp_free_armor 0",
                "mp_halftime 1",
                "mp_limitteams 0",
                "mp_t_default_secondary weapon_glock",
                "mp_team_intro_time 6.5",
                "mp_team_timeout_max 0",
                "mp_technical_timeout_per_team 0",
                "mp_warmuptime_all_players_connected 0",
                "mp_weapons_allow_typecount -1",
                "sv_hibernate_when_empty 0",
                "tv_enable_dynamic 1",
                // Voting convars
                "sv_allow_votes 0",
                "sv_vote_allow_spectators 0",
                "sv_vote_allow_in_warmup 0",
                "sv_vote_issue_kick_allowed 0",
                "sv_vote_issue_loadbackup_allowed 0",
                "sv_vote_issue_surrrender_allowed 0",
                "sv_vote_issue_pause_match_allowed 0",
                "sv_vote_issue_timeout_allowed 0",
                // Demo settings
                "tv_record_immediate 1",
                // Team lock convars
                $"mp_force_pick_time {(lockTeams ? 0 : 15)}",
                $"sv_disable_teamselect_menu {(lockTeams ? 1 : 0)}",
                // Backup convars
                $"mp_backup_round_file \"\"",
                // ...then run these
                warmupTime > -1
                    ? $"mp_warmup_pausetimer 0;mp_warmuptime {warmupTime}"
                    : "mp_warmup_pausetimer 1",
                "mp_warmup_start"
            ]
        );

    public static void ExecKnife() =>
        ServerX.ExecuteCommand(
            [
                "mp_maxrounds 4",
                "mp_ct_default_secondary \"\"",
                "mp_free_armor 1",
                "mp_freezetime 15",
                "mp_friendlyfire 1",
                "mp_give_player_c4 0",
                "mp_playercashawards 0",
                "mp_round_restart_delay 5",
                "mp_roundtime 120",
                "mp_startmoney 0",
                "mp_t_default_secondary \"\"",
                "mp_team_intro_time 0",
                "mp_teamcashawards 0",
                // Team lock convars
                "mp_force_pick_time 0",
                "sv_disable_teamselect_menu 0",
                // Backup convars
                $"mp_backup_round_file \"\"",
                // ...then run these
                "mp_warmup_end",
                "mp_warmup_pausetimer 0"
            ]
        );

    public static void ExecLive(
        int max_rounds = 30,
        int ot_max_rounds = 6,
        bool friendly_pause = false,
        string? backupPath = null
    ) =>
        ServerX.ExecuteCommand(
            [
                $"mp_backup_round_file {backupPath ?? "\"\""}",
                "ammo_grenade_limit_default 1",
                "ammo_grenade_limit_flashbang 2",
                "ammo_grenade_limit_total 4",
                "cash_player_bomb_defused 300",
                "cash_player_bomb_planted 300",
                "cash_player_damage_hostage -30",
                "cash_player_interact_with_hostage 300",
                "cash_player_killed_enemy_default 300",
                "cash_player_killed_enemy_factor 1",
                "cash_player_killed_hostage -1000",
                "cash_player_killed_teammate -300",
                "cash_player_rescued_hostage 1000",
                "cash_team_bonus_shorthanded 0",
                "cash_team_elimination_bomb_map 3250",
                "cash_team_elimination_hostage_map_ct 3000",
                "cash_team_elimination_hostage_map_t 3000",
                "cash_team_hostage_alive 0",
                "cash_team_hostage_interaction 600",
                "cash_team_loser_bonus 1400",
                "cash_team_loser_bonus_consecutive_rounds 500",
                "cash_team_planted_bomb_but_defused 800",
                "cash_team_rescued_hostage 600",
                "cash_team_terrorist_win_bomb 3500",
                "cash_team_win_by_defusing_bomb 3500",
                "cash_team_win_by_hostage_rescue 2900",
                "cash_team_win_by_time_running_out_bomb 3250",
                "cash_team_win_by_time_running_out_hostage 3250",
                "ff_damage_reduction_bullets 0.33",
                "ff_damage_reduction_grenade 0.85",
                "ff_damage_reduction_grenade_self 1",
                "ff_damage_reduction_other 0.4",
                "mp_afterroundmoney 0",
                "mp_autoteambalance 0",
                "mp_backup_restore_load_autopause 1",
                "mp_backup_round_auto 1",
                "mp_buy_anywhere 0",
                "mp_buy_during_immunity 0",
                "mp_buy_allow_grenades 1",
                "mp_buytime 20",
                "mp_c4timer 40",
                "mp_ct_default_melee weapon_knife",
                "mp_ct_default_primary \"\"",
                "mp_ct_default_secondary weapon_hkp2000",
                "mp_death_drop_defuser 1",
                "mp_death_drop_grenade 2",
                "mp_death_drop_gun 1",
                "mp_defuser_allocation 0",
                "mp_disconnect_kills_players 0",
                "mp_display_kill_assists 1",
                "mp_endmatch_votenextmap 0",
                "mp_forcecamera 1",
                "mp_free_armor 0",
                "mp_freezetime 18",
                "mp_friendlyfire 1",
                "mp_give_player_c4 1",
                "mp_halftime 1",
                "mp_halftime_duration 15",
                "mp_halftime_pausetimer 0",
                "mp_ignore_round_win_conditions 0",
                "mp_limitteams 0",
                "mp_match_can_clinch 1",
                "mp_match_end_restart 0",
                "mp_maxmoney 16000",
                $"mp_maxrounds {max_rounds}",
                "mp_overtime_enable 1",
                "mp_overtime_halftime_pausetimer 0",
                $"mp_overtime_maxrounds {ot_max_rounds}",
                "mp_overtime_startmoney 10000",
                "mp_playercashawards 1",
                "mp_randomspawn 0",
                "mp_respawn_immunitytime 0",
                "mp_respawn_on_death_ct 0",
                "mp_respawn_on_death_t 0",
                "mp_round_restart_delay 7",
                "mp_roundtime 1.92",
                "mp_roundtime_defuse 1.92",
                "mp_roundtime_hostage 1.92",
                "mp_solid_teammates 1",
                "mp_spectators_max 20",
                "mp_starting_losses 1",
                "mp_startmoney 800",
                "mp_t_default_melee weapon_knife",
                "mp_t_default_primary \"\"",
                "mp_t_default_secondary weapon_glock",
                "mp_team_intro_time 6.5",
                "mp_team_timeout_max 4",
                "mp_team_timeout_time 30",
                "mp_teamcashawards 1",
                "mp_timelimit 0",
                "mp_weapons_allow_map_placed 1",
                "mp_weapons_allow_typecount 5",
                "mp_weapons_allow_zeus 1",
                "mp_win_panel_display_time 3",
                "spec_freeze_deathanim_time 0",
                "spec_freeze_time 2",
                "spec_freeze_time_lock 2",
                "spec_replay_enable 0",
                "sv_allow_votes 1",
                "sv_auto_full_alltalk_during_warmup_half_end 0",
                "sv_deadtalk 1",
                "sv_ignoregrenaderadio 0",
                "sv_talk_enemy_dead 0",
                "sv_talk_enemy_living 0",
                "sv_voiceenable 1",
                "sv_vote_command_delay 0",
                "tv_relayvoice 1",
                "mp_warmuptime_all_players_connected 0",
                // Pause settings
                $"sv_vote_issue_timeout_allowed {(friendly_pause ? 0 : 1)}",
                "mp_technical_timeout_per_team 1",
                "mp_team_timeout_max 4",
                "mp_team_timeout_ot_add_each 0",
                "mp_team_timeout_ot_add_once 1",
                "mp_team_timeout_ot_max 1",
                "mp_technical_timeout_duration_s 120",
                "mp_team_timeout_time 30",
                // Demo settings
                $"mp_match_restart_delay 25",
                // ...then end warmup.
                "mp_warmup_end",
                "mp_warmup_pausetimer 0"
            ]
        );
}
