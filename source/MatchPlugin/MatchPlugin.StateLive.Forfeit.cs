/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
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
            Server.PrintToConsole("State::OnMatchCancelled We are no longer forfeiting the match.");
            _isForfeiting = false;
            Match.Plugin.ClearTimer("ForfeitMatch");
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = Match.GetPlayerFromSteamID(@event.Userid?.SteamID);
        if (player != null && !_isForfeiting)
            foreach (var team in Match.Teams)
                if (team.Players.All(p => p.SteamID == player.SteamID || p.Controller == null))
                {
                    Server.PrintToConsole(
                        "StateLive::OnPlayerDisconnect A team is forfeiting the match."
                    );
                    _isForfeiting = true;
                    Match.Plugin.CreateTimer(
                        "ForfeitMatch",
                        Match.forfeit_timeout.Value,
                        () => OnMatchCancelled()
                    );
                }
        return HookResult.Continue;
    }
}
