/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive
{
    private Team? _surrendingTeam;
    private bool _canSurrender = false;

    public void OnSurrenderCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (
            player != null
            && _canSurrender
            && (_surrendingTeam == null || _surrendingTeam == player.Team)
            && !player.Team.SurrenderVotes.Contains(player.SteamID)
        )
        {
            _surrendingTeam = player.Team;
            player.Team.SurrenderVotes.Add(player.SteamID);
            var neededVotes = player.Team.Players.Count(p => p.Controller != null);
            var timerName = $"surrender{player.Team.Index}";
            if (player.Team.SurrenderVotes.Count >= neededVotes)
            {
                if (!_canSurrender)
                    return;
                Match.Plugin.ClearTimer(timerName);
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.surrender_success",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
                player.Team.IsSurrended = true;
                player.Team.Score = 0;
                player.Team.Oppositon.Score = 1;
                UtilitiesX
                    .GetGameRules()
                    ?.TerminateRoundX(
                        0,
                        player.Team.CurrentTeam == CsTeam.Terrorist
                            ? RoundEndReason.TerroristsSurrender
                            : RoundEndReason.CTsSurrender
                    );
            }
            else if (player.Team.SurrenderVotes.Count == 1)
            {
                player.Team.PrintToChat(
                    Match.Plugin.Localizer[
                        "match.surrender_start",
                        Match.GetChatPrefix(),
                        player.Name,
                        neededVotes,
                        Match.surrender_timeout.Value
                    ]
                );
                Match.Plugin.CreateTimer(
                    timerName,
                    Match.surrender_timeout.Value,
                    () =>
                    {
                        _surrendingTeam = null;
                        var hadAllSurrenderVotes =
                            player.Team.SurrenderVotes.Count == player.Team.Players.Count;
                        player.Team.SurrenderVotes.Clear();
                        player.Team.PrintToChat(
                            Match.Plugin.Localizer[
                                hadAllSurrenderVotes
                                    ? "match.surrender_fail1"
                                    : "match.surrender_fail2",
                                Match.GetChatPrefix()
                            ]
                        );
                    }
                );
            }
        }
    }
}
