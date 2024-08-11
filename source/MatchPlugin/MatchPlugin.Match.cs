/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System.IO;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Match
{
    public readonly FakeConVar<string> chat_prefix =
        new("match_chat_prefix", "Prefix for chat messages.", "[{red}Match{default}]");
    public readonly FakeConVar<bool> bots =
        new("match_bots", "Bots join the game to fill slots.", true);
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
        new("match_max_rounds", "Max number of rounds to play.", 6);
    public readonly FakeConVar<int> ot_max_rounds =
        new("match_ot_max_rounds", "Additional rounds to determine winner.", 4);
    public readonly FakeConVar<bool> friendly_pause =
        new("match_friendly_pause", "Teams can pause at any time.", false);
    public readonly FakeConVar<int> knife_vote_timeout =
        new("match_knife_vote_timeout", "Time to decide side.", 60);
    public readonly FakeConVar<int> forfeit_timeout =
        new("match_forfeit_timeout", "Time to forfeit a team.", 60);
    public readonly FakeConVar<int> surrender_timeout =
        new("match_surrender_timeout", "Time to vote surrender.", 30);

    public string? Id = null;
    public string? EventsUrl = null;
    public State State;
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
        State = new(this);
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
        if (EventsUrl != null)
            ServerX.SendJson(EventsUrl, @event);
    }

    public string GetChatPrefix()
    {
        return chat_prefix.Value.ReplaceColorTags();
    }

    public void SetState<T>()
        where T : State
    {
        State.Unload();
        State =
            (T?)Activator.CreateInstance(typeof(T), this)
            ?? throw new InvalidOperationException("Failed to create instance of state.");
        ;
        State.Load();
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
}
