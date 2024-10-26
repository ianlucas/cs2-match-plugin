/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public partial class StateLive
{
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid);
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        OnPlayerConnected(@event.Userid);
        return HookResult.Continue;
    }

    public void OnPlayerConnected(CCSPlayerController? controller)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null && Match.Teams.All(t => t.Players.Any(p => p.Controller != null)))
        {
            _isForfeiting = false;
            Match.Plugin.ClearTimer("ForfeitMatch");
            Match.Log("We are no longer forfeiting the match.");
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null)
            TryForfeitMatch(player);
        return HookResult.Continue;
    }

    public void TryForfeitMatch(Player? disconnecting = null)
    {
        if (!_isForfeiting)
            foreach (var team in Match.Teams)
                if (
                    team.Players.All(p =>
                        p.SteamID == disconnecting?.SteamID || p.Controller == null
                    )
                )
                {
                    _isForfeiting = true;
                    Match.Plugin.CreateTimer(
                        "ForfeitMatch",
                        Match.forfeit_timeout.Value,
                        OnMatchCancelled
                    );
                    Match.Log("A team is forfeiting the match.");
                    return;
                }
    }
}
