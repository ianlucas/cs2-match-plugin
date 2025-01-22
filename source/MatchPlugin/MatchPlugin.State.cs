/*---------------------------------------------------------------------------------------------
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

    public virtual string Name { get; set; } = "default_state";

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
            Team? winner = null;

            foreach (var team in Match.Teams)
            {
                if (team.IsSurrended)
                {
                    result = MapResult.Forfeited;
                    winner = team.Opposition;
                    Match.Log($"forfeited, result={result}, winner={winner.Index}");
                    break;
                }
                if (team.Score > team.Opposition.Score)
                {
                    result = MapResult.Completed;
                    winner = team;
                    Match.Log($"completed, result={result}, winner={winner.Index}");
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
            var loser = winner.Opposition;
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

    public void OnMapEnd(MapResult result = MapResult.None, Team? winner = null)
    {
        Match.Log($"Map has ended, result={result}.");
        var map = Match.GetMap() ?? new(Server.MapName);
        var stats = Match.Teams.Select(t => t.Players.Select(p => p.Stats).ToList()).ToList();
        var demoFilename = Match.Cstv.GetFilename();
        var scores = Match.Teams.Select(t => t.Score).ToList();
        var team1 = Match.Teams.First();
        var team2 = team1.Opposition;

        map.DemoFilename = demoFilename;
        map.KnifeRoundWinner = Match.KnifeRoundWinner?.Index;
        map.Result = result;
        map.Stats = stats;
        map.Winner = winner;
        map.Scores = scores;

        if (winner != null)
            winner.SeriesScore += 1;

        var maps = (Match.Maps.Count > 0 ? Match.Maps : [map]).Where(m =>
            m.Result != MapResult.None
        );

        // Even with Get5 Events, we still store results in json for further debugging.
        // @todo Maybe only save if `match_verbose` is enabled in the future.
        ServerX.WriteJson(ServerX.GetConfigPath($"{Match.GetMatchFolder()}/results.json"), maps);
        Match.SendEvent(Match.Get5.OnMapResult(map));

        var mapCount = Match.Maps.Count;
        if (mapCount % 2 == 0)
            mapCount += 1;
        var seriesScoreToWin = (int)Math.Round(mapCount / 2.0, MidpointRounding.AwayFromZero);
        var isSeriesCancelled = result != MapResult.Completed;
        var isSeriesOver =
            isSeriesCancelled
            || (Match.ClinchSeries && Match.Teams.Any(t => t.SeriesScore >= seriesScoreToWin))
            || Match.GetMap() == null;

        if (isSeriesOver)
        {
            // If match doesn't end normally, we already decided which side won.
            if (isSeriesCancelled)
            {
                team1.SeriesScore = 0;
                team2.SeriesScore = 0;
                if (winner != null)
                    winner.SeriesScore = 1;
            }

            // Team with most series score wins the series for non clinch series.
            if (!Match.ClinchSeries)
            {
                winner =
                    team1.SeriesScore > team2.SeriesScore
                        ? team1
                        : team2.SeriesScore > team1.SeriesScore
                            ? team2
                            : null;
            }

            Match.SendEvent(Match.Get5.OnSeriesResult(winner));
            Match.Reset();
            Match.Log($"Match is over, kicking players={Match.matchmaking.Value}");
            Match.Plugin.OnMatchMatchmakingChanged(null, Match.matchmaking.Value);
            if (Match.matchmaking.Value)
                foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
                    controller.Kick();
        }

        // Demo will be stopped at StateWarmupReady::Load.
        if (Match.Cstv.IsRecording())
        {
            var filename = Match.GetDemoFilename();
            if (filename != null)
                Match.SendEvent(Match.Get5.OnDemoFinished(filename));
        }

        Match.SetState(isSeriesOver ? new StateNone() : new StateWarmupReady());
    }
}
