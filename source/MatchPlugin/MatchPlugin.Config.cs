/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class Config
{
    public static void ExecWarmup(int warmupTime = -1) =>
        ServerExt.ExecuteCommand(
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
}
