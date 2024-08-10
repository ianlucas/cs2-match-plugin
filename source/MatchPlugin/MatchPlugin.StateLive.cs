/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive(Match match) : State(match)
{
    public static readonly List<string> PauseCmds = ["css_pause", "css_p", "css_pausar"];
    public static readonly List<string> UnpauseCmds = ["css_unpause", "css_up", "css_despausar"];
    public static readonly List<string> SurrenderCmds = ["css_gg", "css_desistir"];

    public override void Load()
    {
        SurrenderCmds.ForEach(c => Match.Plugin.AddCommand(c, "Surrender", OnSurrenderCommand));
        PauseCmds.ForEach(c => Match.Plugin.AddCommand(c, "Pause the match", OnPauseCommand));
        UnpauseCmds.ForEach(c => Match.Plugin.AddCommand(c, "Unpause the match", OnUnpauseCommand));
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch);

        Config.ExecLive(
            max_rounds: Match.max_rounds.Value,
            ot_max_rounds: Match.ot_max_rounds.Value
        );

        var localize = Match.Plugin.Localizer;
        ServerX.PrintToChatAllRepeat(localize["match.live", Match.GetChatPrefix()]);
        Server.PrintToChatAll(localize["match.live_disclaimer", Match.GetChatPrefix()]);

        foreach (var team in Match.Teams)
            team.IsSurrended = false;

        Match.CurrentRound = 0;
    }

    public override void Unload()
    {
        Match.Plugin.ClearAllTimers();
        SurrenderCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnSurrenderCommand));
        PauseCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnPauseCommand));
        UnpauseCmds.ForEach(c => Match.Plugin.RemoveCommand(c, OnUnpauseCommand));
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        _canSurrender = true;
        Match.CurrentRound += 1;
        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo _)
    {
        var attacker = Match.GetPlayerFromSteamID(@event.Attacker?.SteamID);
        var victim = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (attacker != null && victim != null)
        {
            if (victim.DamageReport.TryGetValue(attacker.SteamID, out var attackerDamageReport))
            {
                attackerDamageReport.From.Value += @event.DmgHealth;
                attackerDamageReport.From.Value = Math.Min(attackerDamageReport.From.Value, 100);
                attackerDamageReport.From.Hits += 1;
            }
            if (attacker.DamageReport.TryGetValue(victim.SteamID, out var victimDamageReport))
            {
                victimDamageReport.To.Value += @event.DmgHealth;
                victimDamageReport.To.Value = Math.Min(victimDamageReport.To.Value, 100);
                victimDamageReport.To.Hits += 1;
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event, GameEventInfo _)
    {
        _canSurrender = false;
        var localize = Match.Plugin.Localizer;
        var home = Match.Teams.First();
        var away = Match.Teams.Last();
        Server.PrintToChatAll(
            localize[
                "match.round_end_score",
                Match.GetChatPrefix(),
                home.FormattedName,
                home.Score,
                away.Score,
                away.FormattedName
            ]
        );
        foreach (var player in Match.Teams.SelectMany(t => t.Players))
        {
            foreach (var report in player.DamageReport.Values)
            {
                player.Controller?.PrintToChat(
                    localize[
                        "match.round_end_damage",
                        Match.GetChatPrefix(),
                        report.To.Value,
                        report.To.Hits,
                        report.From.Value,
                        report.From.Hits,
                        report.Player.Name,
                        report.Player.Controller?.GetHealth() ?? 0
                    ]
                );
                report.Reset();
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch @event, GameEventInfo _)
    {
        var result = MapResult.None;
        int? winner = null;
        foreach (var team in Match.Teams)
        {
            if (team.IsSurrended)
            {
                result = MapResult.Forfeited;
                winner = team.Oppositon.Index;
                Server.PrintToConsole(
                    $"StateLive::OnCsWinPanelMatch forfeited, result={result}, winner={winner}"
                );
                break;
            }
            if (team.Score > team.Oppositon.Score)
            {
                result = MapResult.Completed;
                winner = team.Index;
                Server.PrintToConsole(
                    $"StateLive::OnCsWinPanelMatch completed, result={result}, winner={winner}"
                );
            }
        }
        var mp_match_restart_delay = ConVar
            .Find("mp_match_restart_delay")
            ?.GetPrimitiveValue<int>();
        var interval = mp_match_restart_delay != null ? mp_match_restart_delay - 2 : 1;
        Match.Plugin.CreateTimer("matchend", (float)interval, OnMatchEnd);
        // @todo: Kick players if matchmaking.
        return HookResult.Continue;
    }

    public void OnMatchEnd()
    {
        if (!Match.LoadedFromFile)
            Match.Reset();
        Match.SetState<StateWarmupReady>();
    }
}
