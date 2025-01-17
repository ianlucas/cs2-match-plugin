/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive : State
{
    public override string Name => "live";

    public static readonly List<string> PauseCmds = ["css_pause", "css_p", "css_pausar"];
    public static readonly List<string> UnpauseCmds = ["css_unpause", "css_up", "css_despausar"];
    public static readonly List<string> SurrenderCmds = ["css_gg", "css_desistir"];

    public long RoundStartedAt = 0;

    private bool _isForfeiting = false;
    private bool _isLastRoundBeforeHalfTime = false;
    private readonly Dictionary<ulong, int> _playerHealth = [];
    private readonly Dictionary<uint, UtilityVictims> _utilityVictims = [];

    public override void Load()
    {
        SurrenderCmds.ForEach(c => AddCommand(c, "Surrender", OnSurrenderCommand));
        PauseCmds.ForEach(c => AddCommand(c, "Pause the match", OnPauseCommand));
        UnpauseCmds.ForEach(c => AddCommand(c, "Unpause the match", OnUnpauseCommand));
        AddCommand("css_restore", "Restore a round.", OnRestoreCommand);
        Match.Plugin.RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        Match.Plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Match.Plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        Match.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.RegisterEventHandler<EventRoundStart>(Stats_OnRoundStart);
        Match.Plugin.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        Match.Plugin.RegisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        Match.Plugin.RegisterEventHandler<EventPlayerBlind>(Stats_OnPlayerBlind);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Post);
        Match.Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.RegisterEventHandler<EventPlayerDeath>(Stats_OnPlayerDeath);
        Match.Plugin.RegisterEventHandler<EventBombPlanted>(Stats_OnBombPlanted);
        Match.Plugin.RegisterEventHandler<EventBombDefused>(Stats_OnBombDefused);
        Match.Plugin.RegisterEventHandler<EventRoundMvp>(Stats_OnRoundMvp);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        Match.Plugin.RegisterEventHandler<EventRoundEnd>(Stats_OnRoundEnd);
        Match.Plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        Match.Log("Execing Live");
        Match.SendEvent(Match.Get5.OnGoingLive());

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

        TryForfeitMatch();
    }

    public override void Unload()
    {
        RemoveAllCommands();

        Match.Plugin.ClearAllTimers();
        Match.Plugin.DeregisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        Match.Plugin.DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Match.Plugin.DeregisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        Match.Plugin.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Match.Plugin.DeregisterEventHandler<EventRoundStart>(Stats_OnRoundStart);
        Match.Plugin.DeregisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        Match.Plugin.DeregisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        Match.Plugin.DeregisterEventHandler<EventPlayerBlind>(Stats_OnPlayerBlind);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Post);
        Match.Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.DeregisterEventHandler<EventPlayerDeath>(Stats_OnPlayerDeath);
        Match.Plugin.DeregisterEventHandler<EventBombPlanted>(Stats_OnBombPlanted);
        Match.Plugin.DeregisterEventHandler<EventBombDefused>(Stats_OnBombDefused);
        Match.Plugin.DeregisterEventHandler<EventRoundMvp>(Stats_OnRoundMvp);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        Match.Plugin.DeregisterEventHandler<EventRoundEnd>(Stats_OnRoundEnd);
        Match.Plugin.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        var gameRules = UtilitiesX.GetGameRules();
        RoundStartedAt = ServerX.NowMilliseconds();

        _canSurrender = true;
        _isLastRoundBeforeHalfTime = gameRules.IsLastRoundBeforeHalfTime();
        _playerHealth.Clear();

        // @todo validate that this is working as expected, I don't think it's triggered mid freezetime.

        if (
            gameRules.TechnicalTimeOut
            || gameRules.TerroristTimeOutActive
            || gameRules.CTTimeOutActive
        )
        {
            var team1 = Match.Teams.First();
            CsTeam? csTeamTimeoutActive = gameRules.TerroristTimeOutActive
                ? CsTeam.Terrorist
                : gameRules.CTTimeOutActive
                    ? CsTeam.CounterTerrorist
                    : null;
            var pauseTeam =
                csTeamTimeoutActive != null
                    ? team1.CurrentTeam == csTeamTimeoutActive
                        ? team1
                        : team1.Opposition
                    : null;
            Match.SendEvent(
                Match.Get5.OnPauseBegan(
                    team: pauseTeam,
                    pauseType: gameRules.TechnicalTimeOut ? "technical" : "tactical"
                )
            );
        }

        Match.SendEvent(Match.Get5.OnRoundStart());

        return HookResult.Continue;
    }

    public HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
        {
            Match.SendEvent(Match.Get5.OnGrenadeThrown(player, weapon: @event.Weapon));
        }
        return HookResult.Continue;
    }

    public HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null && _utilityVictims.TryGetValue((uint)@event.Entityid, out var victims))
        {
            var roundTime = ServerX.NowMilliseconds() - RoundStartedAt;
            Match.Plugin.AddTimer(
                0.001f,
                () =>
                {
                    var gameRules = UtilitiesX.GetGameRules();

                    Match.SendEvent(
                        Match.Get5.OnHEGrenadeDetonated(player, weapon: "weapon_hegrenade", victims)
                    );
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo _)
    {
        var attacker = Match.GetPlayerFromSteamID(@event.Attacker?.SteamID);
        var victim = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (attacker != null && victim != null)
        {
            var damage = Math.Max(
                0,
                Math.Min(
                    @event.DmgHealth,
                    _playerHealth.TryGetValue(victim.SteamID, out var health) ? health : 100
                )
            );
            if (victim.DamageReport.TryGetValue(attacker.SteamID, out var attackerDamageReport))
            {
                attackerDamageReport.From.Value += damage;
                attackerDamageReport.From.Hits += 1;
            }
            if (attacker.DamageReport.TryGetValue(victim.SteamID, out var victimDamageReport))
            {
                victimDamageReport.To.Value += damage;
                victimDamageReport.To.Hits += 1;
            }
            Stats_OnPlayerHurt(@event, damage);
            _playerHealth[victim.SteamID] = Math.Max(0, @event.Health);
        }
        return HookResult.Continue;
    }

    public HookResult OnTakeDamage(DynamicHook hook)
    {
        var entity = hook.GetParam<CEntityInstance>(0);

        if (entity.DesignerName != "player")
            return HookResult.Continue;

        var info = hook.GetParam<CTakeDamageInfo>(1);
        var inflictor = info.Inflictor.Value;

        if (inflictor == null || !ItemUtilities.IsUtilityClassname(inflictor.DesignerName))
            return HookResult.Continue;

        var pawn = entity.As<CCSPlayerPawn>();
        var controller = pawn.Controller.Value?.As<CCSPlayerController>();
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);

        if (player != null && controller != null)
        {
            var victims = _utilityVictims.TryGetValue(inflictor.Index, out var v) ? v : [];
            var victim = victims.TryGetValue(player.SteamID, out var p) ? p : new(player, false, 0);
            if (controller.GetHealth() <= 0)
                victim.Killed = true;
            victim.Damage += (int)info.Damage;
            victims[player.SteamID] = victim;
            _utilityVictims[inflictor.Index] = victims;
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

public class UtilityDamage(Player player, bool killed, int damage)
{
    public Player Player = player;
    public bool Killed = killed;
    public int Damage = damage;
}

public class UtilityVictims : Dictionary<ulong, UtilityDamage> { }
