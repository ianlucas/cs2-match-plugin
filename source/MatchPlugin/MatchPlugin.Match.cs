/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace MatchPlugin;

public class Match
{
    public readonly FakeConVar<string> chat_prefix =
        new("match_chat_prefix", "Prefix for chat messages.", "[{red}Match{default}]");
    public readonly FakeConVar<bool> bots =
        new("match_bots", "Bots join the game to fill slots.", false);
    public readonly FakeConVar<bool> tv_record =
        new("match_tv_record", "Are we recording demos?", true);
    public readonly FakeConVar<int> tv_delay =
        new("match_tv_delay", "CSTV's broadcast delay.", 105);
    public readonly FakeConVar<int> players_needed =
        new("match_players_needed", "Number of players needed for a match.", 10);
    public readonly FakeConVar<int> players_needed_per_team =
        new("match_players_needed_per_team", "Number of players needed per team.", 5);
    public readonly FakeConVar<bool> matchmaking =
        new("match_matchmaking", "Matchmaking mode", false);
    public readonly FakeConVar<int> matchmaking_ready_timeout =
        new("match_matchmaking_ready_timeout", "Time to players ready up.", 300);
    public readonly FakeConVar<int> max_rounds =
        new("match_max_rounds", "Max number of rounds to play.", 24);
    public readonly FakeConVar<int> ot_max_rounds =
        new("match_ot_max_rounds", "Additional rounds to determine winner.", 6);
    public readonly FakeConVar<bool> friendly_pause =
        new("match_friendly_pause", "Teams can pause at any time.", false);
    public readonly FakeConVar<int> knife_vote_timeout =
        new("match_knife_vote_timeout", "Time to decide side.", 60);
    public readonly FakeConVar<int> forfeit_timeout =
        new("match_forfeit_timeout", "Time to forfeit a team.", 60);
    public readonly FakeConVar<int> surrender_timeout =
        new("match_surrender_timeout", "Time to vote surrender.", 30);
    public readonly FakeConVar<bool> verbose =
        new("match_verbose", "Are we debugging the plugin?", true);
    public readonly FakeConVar<string> server_id =
        new("get5_server_id", "A string that identifies your server.", "");
    public readonly FakeConVar<string> remote_log_url =
        new("get5_remote_log_url", "The URL to send all events to.", "");
    public readonly FakeConVar<string> remote_log_header_key =
        new("get5_remote_log_header_key", "Key of the header sent on remote log request.", "");
    public readonly FakeConVar<string> remote_log_header_value =
        new("get5_remote_log_header_value", "Value of the header sent on remote log request.", "");

    public readonly MatchPlugin Plugin;
    public readonly List<Team> Teams = [];
    public readonly List<Map> Maps = [];
    public readonly Team Team1;
    public readonly Team Team2;
    public readonly CSTV Cstv;
    public readonly Get5 Get5;

    public string? Id = null;
    public bool IsClinchSeries = true;
    public State State = new();
    public bool IsLoadedFromFile = false;
    public bool IsSeriesStarted = false;
    public Team? KnifeRoundWinner;

    public Match(MatchPlugin plugin)
    {
        var terrorists = new Team(this, CsTeam.Terrorist);
        var cts = new Team(this, CsTeam.CounterTerrorist);
        terrorists.Opposition = cts;
        cts.Opposition = terrorists;
        Teams = [terrorists, cts];
        Team1 = terrorists;
        Team2 = cts;
        Plugin = plugin;
        Cstv = new(this);
        Get5 = new(this);
    }

    public void Reset()
    {
        Id = null;
        IsClinchSeries = true;
        IsLoadedFromFile = false;
        IsSeriesStarted = false;
        KnifeRoundWinner = null;
        Maps.Clear();
        foreach (var team in Teams)
            team.Reset();
    }

    public void SendEvent(object data)
    {
        var url = remote_log_url.Value.StripQuotes();
        PropertyInfo? propertyInfo = data.GetType().GetProperty("event");
        Log($"RemoteLogUrl='{url}' event='{propertyInfo?.GetValue(data)}'");

        if (url != "")
        {
            var headers = new Dictionary<string, string>();
            if (server_id.Value != "")
                headers.Add("Get5-ServerId", server_id.Value);
            if (remote_log_header_key.Value != "" && remote_log_header_value.Value != "")
                headers.Add(remote_log_header_key.Value, remote_log_header_value.Value);
            ServerX.SendJson(url, data, headers);
        }
    }

    public string GetChatPrefix(bool stripColors = false)
    {
        return stripColors
            ? chat_prefix.Value.StripColorTags()
            : chat_prefix.Value.ReplaceColorTags();
    }

    public void SetState(State newState)
    {
        if (newState is not StateWarmupReady && State.GetType() == newState.GetType())
            return;
        SendEvent(Get5.OnGameStateChanged(oldState: State, newState));
        State.Unload();
        Log($"Unloaded {State.GetType().FullName}");
        State = newState;
        State.Match = this;
        State.Load();
        Log($"Loaded {State.GetType().FullName}");
    }

    public Team? GetTeamFromCsTeam(CsTeam? csTeam)
    {
        return Teams.FirstOrDefault(t => t.StartingTeam == csTeam);
    }

    public Player? GetPlayerFromSteamID(ulong? steamId)
    {
        return Teams.SelectMany(t => t.Players).FirstOrDefault(p => p.SteamID == steamId);
    }

    public void RemovePlayerBySteamID(ulong? steamId)
    {
        var player = GetPlayerFromSteamID(steamId);
        player?.Team.RemovePlayer(player);
    }

    public bool IsMatchmaking()
    {
        return IsLoadedFromFile && matchmaking.Value;
    }

    public Map? GetMap() => Maps.Where(m => m.Result == MapResult.None).FirstOrDefault();

    public int GetMapIndex()
    {
        try
        {
            var map = GetMap();
            if (map == null)
                return 0;
            return Maps.IndexOf(map);
        }
        catch
        {
            Log($"This is a bug. Unable to find index for the current map index.");
            return 0;
        }
    }

    public int FindMapIndex(Map? map)
    {
        try
        {
            return map != null ? Maps.IndexOf(map) : 0;
        }
        catch
        {
            Log($"This is a bug. Unable to find index for map {map?.MapName}.");
            return 0;
        }
    }

    public int GetNeededPlayers() =>
        IsLoadedFromFile ? Teams.SelectMany(t => t.Players).Count() : players_needed.Value;

    public bool AreTeamsLocked()
    {
        return IsLoadedFromFile || State is not StateWarmupReady;
    }

    public string GetMatchFolder() => Id != null ? $"/{(IsLoadedFromFile ? "M_" : "S_")}{Id}" : "";

    public DirectoryInfo CreateMatchFolder() =>
        Directory.CreateDirectory(ServerX.GetConfigPath(GetMatchFolder()));

    public string? GetBackupPrefix() =>
        Id != null ? ServerX.GetConfigConVarPath($"{GetMatchFolder()}/{Server.MapName}") : null;

    public string? GetDemoFilename() =>
        Id != null ? ServerX.GetConfigConVarPath($"{GetMatchFolder()}/{Server.MapName}.dem") : null;

    public bool AreTeamsPlayingSwitchedSides() =>
        State is StateLive && UtilitiesX.GetGameRules().AreTeamsPlayingSwitchedSides();

    public void Setup()
    {
        if (Id == "" || !IsLoadedFromFile)
            Id = Guid.NewGuid().ToString();

        if (!IsLoadedFromFile)
            Maps.Add(new(Server.MapName));

        var idsInMatch = Teams.SelectMany(t => t.Players).Select(p => p.SteamID);
        foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
            if (!idsInMatch.Contains(controller.SteamID))
                if (
                    !matchmaking.Value
                    || AdminManager.PlayerHasPermissions(controller, "@css/config")
                )
                    controller.ChangeTeam(CsTeam.Spectator);
                else
                    controller.Kick();

        foreach (var team in Teams)
        {
            ServerX.SetTeamName(team.StartingTeam, team.ServerName);
            foreach (var player in team.Players)
            {
                player.DamageReport.Clear();
                foreach (var opponent in team.Opposition.Players)
                    player.DamageReport.Add(opponent.SteamID, new(opponent));
            }
        }

        IsSeriesStarted = true;
        CreateMatchFolder();
        SendEvent(Get5.OnSeriesInit());
    }

    public bool CheckCurrentMap()
    {
        var currentMap = GetMap();
        if (currentMap != null && Server.MapName != currentMap.MapName)
        {
            Log($"Need to change map to {currentMap.MapName}");
            Server.ExecuteCommand($"changelevel {currentMap.MapName}");
            return true;
        }
        return false;
    }

    public long GetRoundTime() =>
        State is StateLive state ? ServerX.NowMilliseconds() - state.RoundStartedAt : 0;

    public int GetRoundNumber() =>
        State is StateLive state
            ? state.Round > -1
                ? state.Round
                : 0
            : 0;

    public void Log(string message, bool printToChat = false)
    {
        if (printToChat)
            Server.PrintToChatAll(message);
        if (!verbose.Value)
            return;
        var stackTrace = new StackTrace();
        var frame = stackTrace.GetFrame(1);
        var method = frame?.GetMethod();
        var className = method?.DeclaringType?.Name;
        var methodName = method?.Name;
        var prefix =
            className != null && methodName != null ? $"{className}::{methodName}" : "MatchPlugin";
        var output = $"{prefix} {message}";
        Plugin.Logger.LogInformation(output);
    }
}
