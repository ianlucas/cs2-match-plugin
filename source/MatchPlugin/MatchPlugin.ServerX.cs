/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;

namespace MatchPlugin;

public class ServerX
{
    public static void ExecuteCommand(List<string> commands) =>
        commands.ForEach(Server.ExecuteCommand);

    public static void PrintToChatAllRepeat(string message, int amount = 3)
    {
        for (var n = 0; n < amount; n++)
            Server.PrintToChatAll(message);
    }
}
