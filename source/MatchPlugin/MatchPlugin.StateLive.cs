/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public partial class StateLive : State
{
    public static readonly List<string> PauseCmds = ["css_pause", "css_p", "css_pausar"];
    public static readonly List<string> UnpauseCmds = ["css_unpause", "css_up", "css_despausar"];
    public static readonly List<string> SurrenderCmds = ["css_gg", "css_desistir"];

    private bool _isForfeiting = false;

    public override void Load()
    {
        SurrenderCmds.ForEach(c => AddCommand(c, "Surrender", OnSurrenderCommand));
        PauseCmds.ForEach(c => AddCommand(c, "Pause the match", OnPauseCommand));
        UnpauseCmds.ForEach(c => AddCommand(c, "Unpause the match", OnUnpauseCommand));
        AddCommand("css_restore", "Restore a round.", OnRestoreCommand);
        Match.Plugin.RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        Match.Plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        Match.Plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        Match.Log("Execing Live");
        Config.ExecLive(
            max_rounds: Match.max_rounds.Value,
            ot_max_rounds: Match.ot_max_rounds.Value,
            backupPath: Match.GetBackupPrefix(),
            restartDelay: Match.Cstv.IsRecording() ? Match.tv_delay.Value + 5 : 25
        );

        var localize = Match.Plugin.Localizer;
        ServerX.PrintToChatAllRepeat(localize["match.live", Match.GetChatPrefix()]);
        Server.PrintToChatAll(localize["match.live_disclaimer", Match.GetChatPrefix()]);

        foreach (var team in Match.Teams)
            team.IsSurrended = false;

        UtilitiesX.RemovePlayerClans();

        Match.CurrentRound = 0;
    }

    public override void Unload()
    {
        RemoveAllCommands();

        Match.Plugin.ClearAllTimers();
        Match.Plugin.DeregisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        Match.Plugin.DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Match.Plugin.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        Match.Plugin.DeregisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        Match.Plugin.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
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
}
