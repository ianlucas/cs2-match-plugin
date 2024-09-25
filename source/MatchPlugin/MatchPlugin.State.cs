/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class State
{
    private Match? _match = null;

    protected bool _matchCancelled = false;

    private readonly List<CommandDefinition> _commands = [];

    public Match Match
    {
        get
        {
            if (_match == null)
                throw new Exception("No match assigned.");
            return _match;
        }
        set { _match = value; }
    }

    public virtual void Load() { }

    public virtual void Unload() { }

    public void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
    {
        var definition = new CommandDefinition(name, description, handler);
        Match.Plugin.CommandManager.RegisterCommand(definition);
        _commands.Add(definition);
    }

    public void RemoveAllCommands()
    {
        foreach (var definition in _commands)
            Match.Plugin.CommandManager.RemoveCommand(definition);
        _commands.Clear();
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch @event, GameEventInfo _)
    {
        Match.Plugin.ClearAllTimers();
        var result = MapResult.None;
        int? winner = null;
        foreach (var team in Match.Teams)
        {
            if (team.IsSurrended)
            {
                result = MapResult.Forfeited;
                winner = team.Oppositon.Index;
                Match.Log($"forfeited, result={result}, winner={winner}");
                break;
            }
            if (team.Score > team.Oppositon.Score)
            {
                result = MapResult.Completed;
                winner = team.Index;
                Match.Log($"completed, result={result}, winner={winner}");
            }
        }
        var mp_match_restart_delay =
            ConVar.Find("mp_match_restart_delay")?.GetPrimitiveValue<int>() ?? 25;
        var interval = mp_match_restart_delay - 2;
        Match.Plugin.CreateTimer("matchend", interval, () => OnMapEnd(result, winner));
        return HookResult.Continue;
    }

    public void OnMatchCancelled()
    {
        Match.Log("Match was cancelled.");
        _matchCancelled = true;
        Match.Plugin.ClearAllTimers();
        var winners = Match.Teams.Where(t => t.Players.Any(p => p.Controller != null));
        if (winners.Count() == 1)
        {
            var forfeitedTeam = winners.First().Oppositon;
            forfeitedTeam.IsSurrended = true;
            UtilitiesX
                .GetGameRules()
                .TerminateRoundX(
                    0,
                    forfeitedTeam.CurrentTeam == CsTeam.Terrorist
                        ? RoundEndReason.TerroristsSurrender
                        : RoundEndReason.CTsSurrender
                );
        }
        else
        {
            OnMapEnd(MapResult.Cancelled);
            Match.Reset();
            Match.SetState(new StateWarmupReady());
        }
    }

    public void OnMapEnd(MapResult result = MapResult.None, int? winner = null)
    {
        Match.Log("Map has ended.");
        var map = Match.GetCurrentMap();
        var stats = ServerX.GetLastRoundSaveContents();
        var demoFilename = Match.Cstv.GetFilename();
        if (map != null)
        {
            map.DemoFilename = demoFilename;
            map.KnifeRoundWinner = Match.KnifeRoundWinner?.Index;
            map.Result = result;
            map.Stats = stats;
            map.Winner = winner;
        }
        var maps = (
            Match.Maps.Count > 0
                ? Match.Maps
                :
                [
                    new(Server.MapName)
                    {
                        DemoFilename = demoFilename,
                        KnifeRoundWinner = Match.KnifeRoundWinner?.Index,
                        Result = result,
                        Stats = stats,
                        Winner = winner
                    }
                ]
        ).Where(m => m.Result != MapResult.None);
        ServerX.WriteJson(ServerX.GetFullPath($"{Match.GetMatchFolder()}/results.json"), maps);
        var isSeriesOver = Match.GetCurrentMap() == null;
        if (isSeriesOver || result != MapResult.Completed)
        {
            Match.SendEvent(new { type = "matchend", results = maps });
            Match.Reset();
            Match.Plugin.OnMatchMatchmakingChanged(null, Match.matchmaking.Value);
            if (Match.matchmaking.Value)
                foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
                    controller.Kick();
        }
        Match.SetState(new StateWarmupReady());
    }
}
