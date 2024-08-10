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
    public void OnSurrenderCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null && !player.Team.SurrenderVotes.Contains(player.SteamID))
        {
            player.Team.SurrenderVotes.Add(player.SteamID);
            var neededVotes = player.Team.Players.Count(p => p.Controller != null);
            var timerName = $"surrender{player.Team.Index}";
            if (player.Team.SurrenderVotes.Count >= neededVotes)
            {
                Match.Plugin.ClearTimer(timerName);
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.surrender_success",
                        Match.GetChatPrefix(),
                        player.Team.GetName()
                    ]
                );
                player.Team.IsSurrended = true;
                UtilitiesX
                    .GetGameRules()
                    ?.TerminateRoundX(
                        0,
                        player.Team.GetCurrentTeam() == CsTeam.Terrorist
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
                        player.Team.SurrenderVotes.Clear();
                        player.Team.PrintToChat(
                            Match.Plugin.Localizer["match.surrender_fail", Match.GetChatPrefix()]
                        );
                    }
                );
            }
        }
    }
}
