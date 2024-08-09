/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class MatchPlugin
{
    private bool _pendingInternalPush = true;

    public void OnMapStart(string _)
    {
        _pendingInternalPush = true;
    }

    public void OnTick()
    {
        if (_pendingInternalPush)
        {
            _pendingInternalPush = false;
            OnConfigsExecuted();
        }
        if (_match.bots.Value)
        {
            OnCheckBotOverflowing();
        }
    }

    public void OnConfigsExecuted()
    {
        OnMatchBotsChanged(null, _match.bots.Value);
        _match.SetState<StateWarmupReady>();
    }

    public void OnMatchBotsChanged(object? sender, bool value)
    {
        ServerExt.ExecuteCommand(
            ["bot_quota_mode fill", $"bot_quota {(value ? _match.players_needed.Value : 0)}"]
        );
    }

    public void OnCheckBotOverflowing()
    {
        var neededPerTeam = _match.players_needed_per_team.Value;
        List<IEnumerable<CCSPlayerController>> teams =
        [
            UtilitiesExt.GetPlayersFromTeam(CsTeam.Terrorist),
            UtilitiesExt.GetPlayersFromTeam(CsTeam.CounterTerrorist)
        ];
        foreach (var team in teams)
        {
            int botCount = 0;
            int humanCount = 0;
            int? botToKick = null;
            foreach (var controller in team)
            {
                if (controller.IsBot == true)
                {
                    botCount++;
                    if (botToKick == null && controller.UserId != null)
                        botToKick = controller.UserId;
                }
                else
                    humanCount++;
            }
            if (botCount + humanCount > neededPerTeam && botToKick != null)
                Server.ExecuteCommand($"kickid {botToKick}");
        }
    }
}
