/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class ServerX
{
    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string GetConVarPath(string path = "") =>
        $"addons/counterstrikesharp/configs/plugins/MatchPlugin{path}";

    public static string GetFullPath(string path = "") =>
        Path.Combine(Server.GameDirectory, "csgo", GetConVarPath(path));

    public static void ExecuteCommand(List<string> commands) =>
        commands.ForEach(Server.ExecuteCommand);

    public static void PrintToChatAllRepeat(string message, int amount = 3)
    {
        for (var n = 0; n < amount; n++)
            Server.PrintToChatAll(message);
    }

    public static void SetTeamName(CsTeam team, string name)
    {
        var index = team == CsTeam.CounterTerrorist ? 1 : 2;
        Server.ExecuteCommand($"mp_teamname_{index} {name}");
    }

    public static object? GetLastRoundSaveContents()
    {
        try
        {
            var file = ConVar.Find("mp_backup_round_file_last")?.StringValue;
            if (file == null)
                return null;
            var path = Path.Combine(Server.GameDirectory, "csgo", file);
            return File.Exists(path) ? KeyValues.Parse<object>(File.ReadAllText(path)) : null;
        }
        catch
        {
            return null;
        }
    }
}
