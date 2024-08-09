/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;

namespace MatchPlugin;

public class ServerExt
{
    public static void ExecuteCommand(List<string> commands) =>
        commands.ForEach(Server.ExecuteCommand);
}
