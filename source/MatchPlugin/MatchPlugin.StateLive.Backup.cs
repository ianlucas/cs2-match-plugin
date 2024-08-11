/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive
{
    public void OnRestoreCommand(CCSPlayerController? controller, CommandInfo command)
    {
        if (!AdminManager.PlayerHasPermissions(controller, "@css/config"))
            return;
        if (command.ArgCount < 1 || command.ArgCount > 2)
            return;
        var round =
            command.ArgCount == 1 ? "" : command.ArgByIndex(1).ToLower().Trim().PadLeft(2, '0');
        if (round == "")
            round = ConVar.Find("mp_backup_round_file_last")?.StringValue ?? "";
        else
            round = $"{Match.GetBackupPrefix()}_{round}.txt";
        if (round == "")
            return;
        Server.ExecuteCommand($"mp_backup_restore_load_file {round}");
    }
}
