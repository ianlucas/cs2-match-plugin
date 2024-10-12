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
        if (File.Exists(ServerX.GetCSGOPath(round)))
        {
            Match.Log(
                printToChat: true,
                message: Match.Plugin.Localizer[
                    "match.admin_restore",
                    Match.GetChatPrefix(true),
                    UtilitiesX.GetPlayerName(controller)
                ]
            );
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
