/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class Config
{
    public static void ExecWarmup(int warmupTime = -1) =>
        ServerX.ExecuteCommand(
            [
                "bot_chatter off",
                "bot_join_after_player 0",
                "mp_autoteambalance 0",
                "mp_ct_default_secondary weapon_hkp2000",
                "mp_death_drop_gun 0",
                "mp_free_armor 0",
                "mp_halftime 0",
                "mp_limitteams 0",
                "mp_team_intro_time 6.5",
                "mp_t_default_secondary weapon_glock",
                "mp_weapons_allow_typecount -1",
                "sv_hibernate_when_empty 0",
                "mp_technical_timeout_per_team 0",
                "mp_team_timeout_max 0",
                // Voting convars
                "sv_allow_votes 0",
                "sv_vote_allow_spectators 0",
                "sv_vote_allow_in_warmup 0",
                "sv_vote_issue_kick_allowed 0",
                "sv_vote_issue_loadbackup_allowed 0",
                "sv_vote_issue_surrrender_allowed 0",
                "sv_vote_issue_pause_match_allowed 0",
                "sv_vote_issue_timeout_allowed 0",
                // Team lock convars
                //$"mp_force_pick_time {(_match.IsTeamLocked() ? 0 : 15)}",
                //$"sv_disable_teamselect_menu {(_match.IsTeamLocked() ? 1 : 0)}",
                // Backup convars
                //$"mp_backup_round_file {(_match.IsTeamLocked() ? _match.GetMatchBackupPrefix() : "\"\"")}",
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
                "mp_friendlyfire 0",
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
                //$"mp_backup_round_file {(_match.IsTeamLocked() ? _match.GetMatchBackupPrefix() : "\"\"")}",
                // ...then run these
                "mp_warmup_end",
                "mp_warmup_pausetimer 0"
            ]
        );
}
