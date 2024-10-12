/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.Diagnostics;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Translations;
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

    public string? Id = null;
    public string? EventsUrl = null;
    public State State = new();
    public readonly MatchPlugin Plugin;
    public readonly List<Team> Teams = [];
    public readonly List<Map> Maps = [];
    public bool IsLoadedFromFile = false;
    public Team? KnifeRoundWinner;
    public int CurrentRound = 0;
    public CSTV Cstv;

    public Match(MatchPlugin plugin)
    {
        var terrorists = new Team(this, CsTeam.Terrorist);
        var cts = new Team(this, CsTeam.CounterTerrorist);
        terrorists.Oppositon = cts;
        cts.Oppositon = terrorists;
        Teams = [terrorists, cts];
        Plugin = plugin;
        Cstv = new(this);
    }

    public void Reset()
    {
        Id = null;
        EventsUrl = null;
        IsLoadedFromFile = false;
        CurrentRound = 0;
        KnifeRoundWinner = null;
        Maps.Clear();
        foreach (var team in Teams)
            team.Reset();
    }

    public void SendEvent(object @event)
    {
        PropertyInfo? propertyInfo = @event.GetType().GetProperty("event");
        Log($"type={propertyInfo?.GetValue(@event)}");
        if (EventsUrl != null)
            ServerX.SendJson(EventsUrl, @event);
    }

    public string GetChatPrefix(bool stripColors = false)
    {
        return stripColors
            ? chat_prefix.Value.StripColorTags()
            : chat_prefix.Value.ReplaceColorTags();
    }

    public void SetState(State newState)
    {
        if (newState.GetType() != typeof(StateWarmupReady) && State.GetType() == newState.GetType())
            return;
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

    public Map? GetCurrentMap() => Maps.Where(m => m.Result == MapResult.None).FirstOrDefault();

    public int GetNeededPlayers() =>
        IsLoadedFromFile ? Teams.SelectMany(t => t.Players).Count() : players_needed.Value;

    public bool AreTeamsLocked()
    {
        return IsLoadedFromFile || State is not StateWarmupReady;
    }

    public string GetMatchFolder() =>
        Id != null ? $"/{(IsLoadedFromFile ? "match-" : "scrim-")}{Id}" : "";

    public DirectoryInfo CreateMatchFolder() =>
        Directory.CreateDirectory(ServerX.GetFullPath(GetMatchFolder()));

    public string? GetBackupPrefix() =>
        Id != null ? ServerX.GetConVarPath($"{GetMatchFolder()}/{Server.MapName}") : null;

    public string? GetDemoFilename() =>
        Id != null ? ServerX.GetConVarPath($"{GetMatchFolder()}/{Server.MapName}.dem") : null;

    public bool AreTeamsPlayingSwitchedSides(int? round = null)
    {
        if (State is not StateLive)
            return false;
        round ??= CurrentRound;
        var regulationMaxRound = max_rounds.Value;
        var overtimeRoundsPerSide = ot_max_rounds.Value / 2;
        if (round <= regulationMaxRound)
            return round > regulationMaxRound / 2;
        var overtimeRound = round - regulationMaxRound + (overtimeRoundsPerSide * 5);
        var cycle = (overtimeRound - 1) / (overtimeRoundsPerSide * 2);
        return cycle % 2 == 0
            ? overtimeRound > overtimeRoundsPerSide
            : overtimeRound <= overtimeRoundsPerSide;
    }

    public void Setup()
    {
        if (!IsLoadedFromFile)
        {
            Id = ServerX.Now().ToString();
            CreateMatchFolder();
        }
        var idsInMatch = Teams.SelectMany(t => t.Players).Select(p => p.SteamID);
        foreach (var controller in UtilitiesX.GetPlayersInTeams())
            if (!idsInMatch.Contains(controller.SteamID))
                controller.ChangeTeam(CsTeam.Spectator);
        foreach (var team in Teams)
        {
            ServerX.SetTeamName(team.StartingTeam, team.ServerName);
            foreach (var player in team.Players)
            {
                player.DamageReport.Clear();
                foreach (var opponent in team.Oppositon.Players)
                    player.DamageReport.Add(opponent.SteamID, new(opponent));
            }
        }
    }

    public bool AreTeamsSwitchingSidesNextRound()
    {
        if (State is not StateLive)
            return false;
        return AreTeamsPlayingSwitchedSides() != AreTeamsPlayingSwitchedSides(CurrentRound + 1);
    }

    public bool CheckCurrentMap()
    {
        var currentMap = GetCurrentMap();
        if (currentMap != null && Server.MapName != currentMap.MapName)
        {
            Server.ExecuteCommand($"changelevel {currentMap.MapName}");
            return true;
        }
        return false;
    }

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
        Server.PrintToConsole(output);
    }
}
