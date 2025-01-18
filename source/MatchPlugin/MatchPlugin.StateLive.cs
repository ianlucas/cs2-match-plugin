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
    private uint _lastThrownSmokegrenade = 0;
    private long _bombPlantedAt = 0;
    private int? _lastPlantedBombZone = null;
    private readonly Dictionary<ulong, int> _playerHealth = [];
    private readonly Dictionary<uint, UtilityVictim> _utilityVictims = [];
    private readonly Dictionary<uint, ThrownMolotov> _thrownMolotovs = [];

    // smokegrenade_detonate -> inferno_extinguish -> inferno_expire (always called)
    private readonly Dictionary<uint, bool> _didSmokeExtinguishMolotov = [];

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
        Match.Plugin.RegisterEventHandler<EventDecoyStarted>(OnDecoyStarted);
        Match.Plugin.RegisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        Match.Plugin.RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate);
        Match.Plugin.RegisterEventHandler<EventInfernoStartburn>(OnInfernoStartburn);
        Match.Plugin.RegisterEventHandler<EventInfernoExtinguish>(OnInfernoExtinguish);
        Match.Plugin.RegisterEventHandler<EventInfernoExpire>(OnInfernoExpire);
        Match.Plugin.RegisterEventHandler<EventFlashbangDetonate>(OnFlashbangDetonate);
        Match.Plugin.RegisterEventHandler<EventPlayerBlind>(Stats_OnPlayerBlind);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Post);
        Match.Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.RegisterEventHandler<EventPlayerDeath>(Stats_OnPlayerDeath);
        Match.Plugin.RegisterEventHandler<EventBombPlanted>(Stats_OnBombPlanted);
        Match.Plugin.RegisterEventHandler<EventBombDefused>(Stats_OnBombDefused);
        Match.Plugin.RegisterEventHandler<EventBombExploded>(OnBombExploded);
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
        Match.Plugin.DeregisterEventHandler<EventDecoyStarted>(OnDecoyStarted);
        Match.Plugin.DeregisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        Match.Plugin.DeregisterEventHandler<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate);
        Match.Plugin.DeregisterEventHandler<EventInfernoStartburn>(OnInfernoStartburn);
        Match.Plugin.DeregisterEventHandler<EventInfernoExtinguish>(OnInfernoExtinguish);
        Match.Plugin.DeregisterEventHandler<EventInfernoExpire>(OnInfernoExpire);
        Match.Plugin.DeregisterEventHandler<EventFlashbangDetonate>(OnFlashbangDetonate);
        Match.Plugin.DeregisterEventHandler<EventPlayerBlind>(Stats_OnPlayerBlind);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Post);
        Match.Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Match.Plugin.DeregisterEventHandler<EventPlayerDeath>(Stats_OnPlayerDeath);
        Match.Plugin.DeregisterEventHandler<EventBombPlanted>(Stats_OnBombPlanted);
        Match.Plugin.DeregisterEventHandler<EventBombDefused>(Stats_OnBombDefused);
        Match.Plugin.DeregisterEventHandler<EventBombExploded>(OnBombExploded);
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

        foreach (var molotovEntityId in _thrownMolotovs.Keys)
            SendOnMolotovDetonatedEvent(molotovEntityId);

        _lastThrownSmokegrenade = 0;
        _utilityVictims.Clear();
        _didSmokeExtinguishMolotov.Clear();

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

    public HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
        {
            Match.SendEvent(Match.Get5.OnDecoyStarted(player, weapon: "weapon_decoy"));
        }
        return HookResult.Continue;
    }

    public HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
        {
            var entityId = (uint)@event.Entityid;
            var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
            var roundNumber = Match.GetRoundNumber();
            var roundTime = Match.GetRoundTime();

            Match.Plugin.AddTimer(
                0.001f,
                () =>
                {
                    Match.SendEvent(
                        Match.Get5.OnHEGrenadeDetonated(
                            roundNumber,
                            roundTime,
                            player,
                            weapon: "weapon_hegrenade",
                            victims
                        )
                    );

                    _utilityVictims.Remove(entityId);
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
        {
            var entityId = (uint)@event.Entityid;
            var roundNumber = Match.GetRoundNumber();
            var roundTime = Match.GetRoundTime();

            _lastThrownSmokegrenade = entityId;

            Match.Plugin.AddTimer(
                0.001f,
                () =>
                {
                    Match.SendEvent(
                        Match.Get5.OnSmokeGrenadeDetonated(
                            roundNumber,
                            roundTime,
                            player,
                            weapon: "weapon_smokegrenade",
                            didExtingishMolotovs: _didSmokeExtinguishMolotov.ContainsKey(entityId)
                        )
                    );
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnInfernoStartburn(EventInfernoStartburn @event, GameEventInfo _)
    {
        var entity = Utilities.GetEntityFromIndex<CBaseEntity>(@event.Entityid);
        var pawn = entity?.OwnerEntity.Value?.As<CCSPlayerPawn>();
        var controller = pawn?.Controller.Value?.As<CCSPlayerController>();
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (entity != null && player != null)
            _thrownMolotovs[entity.Index] = new(
                Match.GetRoundNumber(),
                Match.GetRoundTime(),
                player
            );
        return HookResult.Continue;
    }

    public HookResult OnInfernoExtinguish(EventInfernoExtinguish @event, GameEventInfo _)
    {
        _didSmokeExtinguishMolotov[_lastThrownSmokegrenade] = true;
        return HookResult.Continue;
    }

    public HookResult OnInfernoExpire(EventInfernoExpire @event, GameEventInfo _)
    {
        SendOnMolotovDetonatedEvent((uint)@event.Entityid);
        return HookResult.Continue;
    }

    public HookResult OnFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
        {
            var entityId = (uint)@event.Entityid;
            var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
            var roundNumber = Match.GetRoundNumber();
            var roundTime = Match.GetRoundTime();

            Match.Plugin.AddTimer(
                0.001f,
                () =>
                {
                    Match.SendEvent(
                        Match.Get5.OnFlashbangDetonated(
                            roundNumber,
                            roundTime,
                            player,
                            weapon: "weapon_flashbang",
                            victims
                        )
                    );

                    _utilityVictims.Remove(entityId);
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
            var victim = victims.TryGetValue(player.SteamID, out var p) ? p : new(player);
            if (controller.GetHealth() <= 0)
                victim.Killed = true;
            victim.Damage += (int)info.Damage;
            victims[player.SteamID] = victim;
            _utilityVictims[inflictor.Index] = victims;
        }

        return HookResult.Continue;
    }

    public HookResult OnBombExploded(EventBombExploded @event, GameEventInfo _)
    {
        Match.SendEvent(Match.Get5.OnBombExploded(_lastPlantedBombZone));
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

    public void SendOnMolotovDetonatedEvent(uint entityId)
    {
        if (_thrownMolotovs.TryGetValue(entityId, out var thrown))
        {
            var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];

            Match.SendEvent(
                Match.Get5.OnMolotovDetonated(
                    thrown.RoundNumber,
                    thrown.RoundTime,
                    thrown.Player,
                    "weapon_molotov",
                    victims
                )
            );

            _utilityVictims.Remove(entityId);
            _thrownMolotovs.Remove(entityId);
        }
    }
}

public class UtilityDamage(
    Player player,
    bool killed = false,
    int damage = 0,
    bool friendlyFire = false,
    float blindDuration = 0f
)
{
    public Player Player = player;
    public bool Killed = killed;
    public int Damage = damage;
    public bool FriendlyFire = friendlyFire;
    public float BindDuration = blindDuration;
}

public class UtilityVictim : Dictionary<ulong, UtilityDamage> { }

public class ThrownMolotov(int roundNumber, long roundTime, Player player)
{
    public int RoundNumber = roundNumber;
    public long RoundTime = roundTime;
    public Player Player = player;
}
