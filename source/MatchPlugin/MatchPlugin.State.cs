﻿/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
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

    public HookResult OnRoundPrestart(EventRoundPrestart _, GameEventInfo __)
    {
        if (UtilitiesX.GetGameRules().GamePhase == 5)
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

            OnMapEnd(result, winner);
        }

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
            var winner = winners.First();
            var loser = winner.Oppositon;
            loser.IsSurrended = true;
            Match.Log($"Terminating by Cancelled, winner={winner.Index}, forfeited={loser.Index}");
            UtilitiesX
                .GetGameRules()
                .TerminateRoundX(
                    0,
                    loser.CurrentTeam == CsTeam.Terrorist
                        ? RoundEndReason.TerroristsSurrender
                        : RoundEndReason.CTsSurrender
                );
        }
        else
            OnMapEnd(MapResult.Cancelled);
    }

    public void OnMapEnd(MapResult result = MapResult.None, int? winner = null)
    {
        Match.Log($"Map has ended, result={result}.");
        var map = Match.GetCurrentMap() ?? new(Server.MapName);
        var stats = Match.Teams.Select(t => t.Players.Select(p => p.Stats).ToList()).ToList();
        var demoFilename = Match.Cstv.GetFilename();
        var scores = Match.Teams.Select(t => t.Score).ToList();

        map.DemoFilename = demoFilename;
        map.KnifeRoundWinner = Match.KnifeRoundWinner?.Index;
        map.Result = result;
        map.Stats = stats;
        map.Winner = winner;
        map.Scores = scores;

        var maps = (Match.Maps.Count > 0 ? Match.Maps : [map]).Where(m =>
            m.Result != MapResult.None
        );

        ServerX.WriteJson(ServerX.GetFullPath($"{Match.GetMatchFolder()}/results.json"), maps);

        var isSeriesOver = Match.GetCurrentMap() == null;
        if (isSeriesOver || result != MapResult.Completed)
        {
            Match.SendEvent(new { type = "matchend", results = maps });
            Match.Reset();
            Match.Log($"Match is over, kicking players={Match.matchmaking.Value}");
            Match.Plugin.OnMatchMatchmakingChanged(null, Match.matchmaking.Value);
            if (Match.matchmaking.Value)
                foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
                    controller.Kick();
        }

        Match.SetState(new StateWarmupReady());
    }
}
