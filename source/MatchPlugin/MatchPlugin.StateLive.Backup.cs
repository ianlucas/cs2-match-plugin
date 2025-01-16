/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace MatchPlugin;

public partial class StateLive
{
    public void OnRestoreCommand(CCSPlayerController? controller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(controller, "@css/config"))
            return;
        if (command.ArgCount != 2)
        {
            controller?.PrintToChat(
                Match.Plugin.Localizer["match.admin_restore_syntax", Match.GetChatPrefix(true)]
            );
            return;
        }
        var round = command.ArgByIndex(1).ToLower().Trim().PadLeft(2, '0');
        round = $"{Match.GetBackupPrefix()}_round{round}.txt";
        var filename = ServerX.GetCSGOPath(round);
        if (File.Exists(filename))
        {
            Match.Log(
                printToChat: true,
                message: Match.Plugin.Localizer[
                    "match.admin_restore",
                    Match.GetChatPrefix(true),
                    UtilitiesX.GetPlayerName(controller)
                ]
            );
            // We load the stats before trying to restore the round. Most cases should work as
            // `mp_backup_restore_load_file` can only fail when the file is not found, but we already had a check
            // for that.
            var players = Match.Teams.SelectMany(t => t.Players);
            foreach (var report in players.SelectMany(p => p.DamageReport.Values))
                report.Reset();
            if (int.TryParse(round, out var roundAsInt))
            {
                if (roundAsInt == 0)
                    foreach (var p in players)
                        p.Stats = new(p.SteamID);
                else if (_statsBackup.TryGetValue(roundAsInt, out var snapshots))
                    foreach (var (player, snapshot) in snapshots)
                        player.Stats = snapshot.Clone();

                Match.SendEvent(Get5Events.OnBackupRestore(Match, roundAsInt, filename));
            }

            Server.ExecuteCommand($"mp_backup_restore_load_file {round}");
        }
        else
            controller?.PrintToChat(
                Match.Plugin.Localizer[
                    "match.admin_restore_error",
                    Match.GetChatPrefix(true),
                    round
                ]
            );
    }
}
