/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class State(Match match)
{
    public readonly Match Match = match;

    protected bool _matchCancelled = false;

    public virtual void Load() { }

    public virtual void Unload() { }

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
                Server.PrintToConsole(
                    $"State::OnCsWinPanelMatch forfeited, result={result}, winner={winner}"
                );
                break;
            }
            if (team.Score > team.Oppositon.Score)
            {
                result = MapResult.Completed;
                winner = team.Index;
                Server.PrintToConsole(
                    $"State::OnCsWinPanelMatch completed, result={result}, winner={winner}"
                );
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
        _matchCancelled = true;
        Match.Plugin.ClearAllTimers();
        var winners = Match.Teams.Where(t => t.Players.Any(p => p.Controller != null));
        if (winners.Count() == 1)
        {
            var forfeitedTeam = winners.First().Oppositon;
            forfeitedTeam.IsSurrended = true;
            UtilitiesX
                .GetGameRules()
                ?.TerminateRoundX(
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
            Match.SetState<StateWarmupReady>();
        }
    }

    public void OnMapEnd(MapResult result = MapResult.None, int? winner = null)
    {
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
        Match.Cstv.Stop();
        Match.SetState<StateWarmupReady>();
    }
}
