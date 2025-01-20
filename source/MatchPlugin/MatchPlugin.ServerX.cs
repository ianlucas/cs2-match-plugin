/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class ServerX
{
    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static long NowMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static string GetConfigConVarPath(string path = "") =>
        $"addons/counterstrikesharp/configs/plugins/MatchPlugin{path}";

    public static string GetConfigPath(string path = "") =>
        Path.Combine(Server.GameDirectory, "csgo", GetConfigConVarPath(path));

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

    public static async void SendJson(
        string url,
        object data,
        Dictionary<string, string>? headers = null
    )
    {
        try
        {
            using HttpClient client = new();
            if (headers != null)
                foreach (var header in headers)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync(url, content);
        }
        catch { }
    }
}
