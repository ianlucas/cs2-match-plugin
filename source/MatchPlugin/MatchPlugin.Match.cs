/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

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
    public readonly FakeConVar<int> players_needed =
        new("match_players_needed", "Number of players needed for a match.", 10);
    public readonly FakeConVar<int> players_needed_per_team =
        new("match_players_needed_per_team", "Number of players needed per team.", 5);
    public readonly FakeConVar<int> max_rounds =
        new("match_max_rounds", "Max number of rounds to play.", 6);
    public readonly FakeConVar<int> ot_max_rounds =
        new("match_ot_max_rounds", "Additional rounds to determine winner.", 4);

    public State State;
    public readonly MatchPlugin Plugin;
    public readonly List<Team> Teams = [];
    public bool LoadedFromFile = false;
    public Team? KnifeRoundWinner;

    public Match(MatchPlugin plugin)
    {
        var terrorists = new Team(this, CsTeam.Terrorist);
        var cts = new Team(this, CsTeam.CounterTerrorist);
        terrorists.Oppositon = cts;
        cts.Oppositon = terrorists;
        Teams = [terrorists, cts];
        Plugin = plugin;
        State = new(this);
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
}
