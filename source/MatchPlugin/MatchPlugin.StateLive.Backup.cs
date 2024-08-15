/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MatchPlugin;

public partial class StateLive
{
    public void OnRestoreCommand(CCSPlayerController? controller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(controller, "@css/config"))
            return;
        var lastBackupFile = ConVar.Find("mp_backup_round_file_last")?.StringValue;
        int lastBackupRound =
            lastBackupFile != null
                ? int.Parse(BackupRoundFilePattern().Match(lastBackupFile).Value)
                : 0;
        if (command.ArgCount != 2)
        {
            var range = lastBackupRound > 0 ? $"0-{lastBackupRound}" : "0";
            controller?.PrintToChat($"Usage: !restore [{range}]");
            return;
        }
        var round = command.ArgByIndex(1).ToLower().Trim().PadLeft(2, '0');
        try
        {
            var roundN = int.Parse(round);
            if (roundN < 0 || roundN > lastBackupRound)
            {
                controller?.PrintToChat("Requested round was not played.");
                return;
            }
        }
        catch
        {
            controller?.PrintToChat("Restore failed. Usage: !restore [round number]");
            return;
        }
        round = $"{Match.GetBackupPrefix()}_round{round}.txt";
        if (File.Exists(ServerX.GetCSGOPath(round)))
            Server.ExecuteCommand($"mp_backup_restore_load_file {round}");
        else
            controller?.PrintToChat($"Restore failed. Unable to find backup file.");
    }

    [GeneratedRegex(@"(?<=round)\d+")]
    private static partial Regex BackupRoundFilePattern();
}
