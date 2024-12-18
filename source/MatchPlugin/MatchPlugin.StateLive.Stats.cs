﻿/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive
{
    private readonly Dictionary<int, List<(Player, PlayerStats)>> _statsBackup = [];
    private readonly Dictionary<CsTeam, bool> _isTeamClutching = [];
    private readonly Dictionary<ulong, int> _roundClutchingCount = [];
    private readonly Dictionary<ulong, int> _roundKills = [];
    private readonly Dictionary<ulong, (ulong, CsTeam, long)> _playerKilledBy = [];
    private bool _hadFirstDeath = false;
    private bool _hadFirstKill = false;

    // KAST
    private readonly Dictionary<ulong, bool> _playerSurvived = [];
    private readonly Dictionary<ulong, bool> _playerKilledOrAssistedOrTradedKill = [];

    public HookResult Stats_OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        _isTeamClutching.Clear();
        _roundClutchingCount.Clear();
        _playerKilledBy.Clear();
        _hadFirstDeath = false;
        _hadFirstKill = false;

        _playerSurvived.Clear();
        _playerKilledOrAssistedOrTradedKill.Clear();

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
        {
            _roundKills[player.SteamID] = 0;
            if (player.Controller != null)
                player.Stats.RoundsPlayed += 1;
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnPlayerBlind(EventPlayerBlind @event, GameEventInfo _)
    {
        var attacker = Match.GetPlayerFromSteamID(@event.Attacker?.SteamID);
        var victim = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);

        if (attacker != null && victim != null)
        {
            var friendlyFire = attacker.Team == victim.Team;
            if (@event.BlindDuration > 2.5)
                if (friendlyFire)
                    attacker.Stats.FriendliesFlashed += 1;
                else
                    attacker.Stats.EnemiesFlashed += 1;
        }

        return HookResult.Continue;
    }

    public void Stats_OnPlayerHurt(EventPlayerHurt @event, int damage)
    {
        var attacker = Match.GetPlayerFromSteamID(@event.Attacker?.SteamID);
        var victim = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (attacker != null && victim != null)
        {
            if (attacker != victim)
            {
                attacker.Stats.Damage += damage;
                if (UtilitiesX.IsUtilityClassname(@event.Weapon))
                    attacker.Stats.UtilDamage += damage;
            }
        }
    }

    public HookResult Stats_OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
    {
        var attacker = Match.GetPlayerFromSteamID(@event.Attacker?.SteamID);
        var victim = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);

        if (victim == null)
            return HookResult.Continue;

        var victimTeam = victim.Team.CurrentTeam;

        if (
            !_isTeamClutching.ContainsKey(victimTeam)
            && UtilitiesX.CountAlivePlayersInTeam(victimTeam) == 1
        )
        {
            _isTeamClutching[victimTeam] = true;
            var clutcher = UtilitiesX.GetAlivePlayersInTeam(victimTeam).FirstOrDefault();
            if (clutcher != null)
                _roundClutchingCount[clutcher.SteamID] = UtilitiesX.CountAlivePlayersInTeam(
                    UtilitiesX.ToggleCsTeam(victimTeam)
                );
        }

        var killedByBomb = @event.Weapon == "planted_c4";
        var killedWithKnife = UtilitiesX.IsKnifeClassname(@event.Weapon);
        var isSuicide = (attacker == null || attacker == victim) && !killedByBomb;
        var headshot = @event.Headshot;

        victim.Stats.Deaths += 1;
        _playerSurvived[victim.SteamID] = true;

        if (!_hadFirstDeath)
        {
            _hadFirstDeath = true;
            if (victimTeam == CsTeam.Terrorist)
                victim.Stats.FirstDeathsT += 1;
            else
                victim.Stats.FirstDeathsCT += 1;
        }

        if (isSuicide)
            victim.Stats.Suicides += 1;
        else if (!killedByBomb)
        {
            if (attacker?.Team == victim.Team)
                attacker.Stats.Teamkills += 1;
            else if (attacker != null)
            {
                var attackerTeam = attacker.Team.CurrentTeam;
                if (!_hadFirstKill)
                {
                    _hadFirstKill = true;
                    if (attackerTeam == CsTeam.Terrorist)
                        attacker.Stats.FirstKillsT += 1;
                    else
                        attacker.Stats.FirstKillsCT += 1;
                }

                _roundKills[attacker.SteamID] += 1;
                _playerKilledBy[victim.SteamID] = (attacker.SteamID, attackerTeam, ServerX.Now());

                foreach (var (aVictim, anAttacker) in _playerKilledBy)
                {
                    if (anAttacker.Item1 == victim.SteamID && victimTeam == attackerTeam)
                    {
                        var delta = ServerX.Now() - anAttacker.Item3;
                        if (delta < 2)
                        {
                            attacker.Stats.TradeKills += 1;
                            _playerKilledOrAssistedOrTradedKill[aVictim] = true;
                        }
                    }
                }

                attacker.Stats.Kills += 1;
                _playerKilledOrAssistedOrTradedKill[attacker.SteamID] = true;

                if (headshot)
                    attacker.Stats.HeadshotKills += 1;

                if (killedWithKnife)
                    attacker.Stats.KnifeKills += 1;

                var assister = Match.GetPlayerFromSteamID(@event.Assister?.SteamID);
                if (assister != null)
                {
                    var friendlyFire = assister.Team == victim.Team;
                    var assistedFlash = @event.Assistedflash;
                    if (!friendlyFire)
                        if (assistedFlash)
                            assister.Stats.FlashbangAssists += 1;
                        else
                        {
                            assister.Stats.Assists += 1;
                            _playerKilledOrAssistedOrTradedKill[assister.SteamID] = true;
                        }
                }
            }
        }

        return HookResult.Continue;
    }

    public HookResult Stats_OnBombPlanted(EventBombPlanted @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
            player.Stats.BombPlants += 1;
        return HookResult.Continue;
    }

    public HookResult Stats_OnBombDefused(EventBombDefused @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
            player.Stats.BombDefuses += 1;
        return HookResult.Continue;
    }

    public HookResult Stats_OnRoundMvp(EventRoundMvp @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
            player.Stats.MVPs += 1;
        return HookResult.Continue;
    }

    public HookResult Stats_OnRoundEnd(EventRoundEnd @event, GameEventInfo _)
    {
        var winner = (CsTeam)@event.Winner;

        var gameRules = UtilitiesX.GetGameRules();
        _statsBackup[gameRules.TotalRoundsPlayed] = [];

        foreach (var player in Match.Teams.SelectMany(t => t.Players))
        {
            if (player.Controller != null)
                player.Stats.Score = player.Controller.Score;

            if (_roundKills.TryGetValue(player.SteamID, out var kills))
                switch (kills)
                {
                    case 1:
                        player.Stats.K1 += 1;
                        break;
                    case 2:
                        player.Stats.K2 += 1;
                        break;
                    case 3:
                        player.Stats.K3 += 1;
                        break;
                    case 4:
                        player.Stats.K4 += 1;
                        break;
                    case 5:
                        player.Stats.K5 += 1;
                        break;
                }

            if (
                player.Team.CurrentTeam == winner
                && _roundClutchingCount.TryGetValue(player.SteamID, out var opponents)
            )
                switch (opponents)
                {
                    case 1:
                        player.Stats.V1 += 1;
                        break;
                    case 2:
                        player.Stats.V2 += 1;
                        break;
                    case 3:
                        player.Stats.V3 += 1;
                        break;
                    case 4:
                        player.Stats.V4 += 1;
                        break;
                    case 5:
                        player.Stats.V5 += 1;
                        break;
                }

            if (
                _playerKilledOrAssistedOrTradedKill.ContainsKey(player.SteamID)
                || _playerSurvived.ContainsKey(player.SteamID)
            )
                player.Stats.KAST += 1;

            _statsBackup[gameRules.TotalRoundsPlayed].Add((player, player.Stats.Clone()));
        }

        return HookResult.Continue;
    }
}
