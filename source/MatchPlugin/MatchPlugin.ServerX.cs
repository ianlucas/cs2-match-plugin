/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class ServerX
{
    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string GetConVarPath(string path = "") =>
        $"addons/counterstrikesharp/configs/plugins/MatchPlugin{path}";

    public static string GetFullPath(string path = "") =>
        Path.Combine(Server.GameDirectory, "csgo", GetConVarPath(path));

    public static string GetCSGOPath(string path = "") =>
        Path.Combine(Server.GameDirectory, "csgo", path);

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

    public static void UpdatePlayersScoreboard()
    {
        try
        {
            new GameEvent("nextlevel_changed", false).FireEvent(false);
        }
        catch { }
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

    public static void WriteJson(string filename, object contents)
    {
        if (File.Exists(filename))
        {
            int version = 1;
            string backupPath;
            do
            {
                backupPath = $"{filename}.{version}";
                version++;
            } while (File.Exists(backupPath));

            File.Copy(filename, backupPath);
        }
        string jsonString = JsonSerializer.Serialize(contents);
        File.WriteAllText(filename, jsonString);
    }

    public static async void SendJson(string url, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpClient client = new();
            await client.PostAsync(url, content);
        }
        catch { }
    }
}
