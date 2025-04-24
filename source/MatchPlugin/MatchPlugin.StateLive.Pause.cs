/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class StateLive
{
    private bool _wasPaused = false;
    private string _wasPausedType = "";
    private Team? _teamWhichPaused = null;

    public void CheckPauseEvents()
    {
        var gameRules = UtilitiesX.GetGameRules();

        if (!gameRules.FreezePeriod)
            return;

        var isTeamPaused = gameRules.TerroristTimeOutActive || gameRules.CTTimeOutActive;
        var isTechnicalPaused = gameRules.TechnicalTimeOut;
        var isMatchPaused = gameRules.MatchWaitingForResume;
        var isPaused = isTeamPaused || isTechnicalPaused || isMatchPaused;
        var didPauseStateChange = _wasPaused != isPaused;

        if (didPauseStateChange)
        {
            if (isPaused)
            {
                CsTeam? sideWhichPaused = gameRules.TerroristTimeOutActive
                    ? CsTeam.Terrorist
                    : gameRules.CTTimeOutActive
                        ? CsTeam.CounterTerrorist
                        : null;
                var teamWhichPaused =
                    sideWhichPaused != null
                        ? Match.Team1.CurrentTeam == sideWhichPaused
                            ? Match.Team1
                            : Match.Team2.Opposition
                        : null;

                var pauseType = isTeamPaused
                    ? "team"
                    : isTechnicalPaused
                        ? "technical"
                        : "admin";

                if (teamWhichPaused != null)
                    Server.PrintToChatAll(
                        Match.Plugin.Localizer[
                            "match.pause_start",
                            Match.GetChatPrefix(),
                            teamWhichPaused.FormattedName
                        ]
                    );

                Match.SendEvent(Match.Get5.OnMatchPaused(team: teamWhichPaused, pauseType));
                Match.SendEvent(Match.Get5.OnPauseBegan(team: teamWhichPaused, pauseType));

                _teamWhichPaused = teamWhichPaused;
                _wasPausedType = pauseType;
            }
            else
                Match.SendEvent(
                    Match.Get5.OnMatchUnpaused(team: _teamWhichPaused, pauseType: _wasPausedType)
                );
        }

        _wasPaused = isPaused;
    }

    public void OnPauseCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null)
        {
            if (Match.friendly_pause.Value)
            {
                foreach (var team in Match.Teams)
                    team.IsUnpauseMatch = false;
                Server.ExecuteCommand("mp_pause_match");
                Match.SendEvent(Match.Get5.OnMatchPaused(team: player.Team, pauseType: "tactical"));
                return;
            }

            controller?.ExecuteClientCommandFromServer("callvote StartTimeOut");
        }
    }

    public void OnUnpauseCommand(CCSPlayerController? controller, CommandInfo _)
    {
        var player = Match.GetPlayerFromSteamID(controller?.SteamID);
        if (player != null && Match.friendly_pause.Value)
        {
            var askedForUnpause = player.Team.IsUnpauseMatch;
            player.Team.IsUnpauseMatch = true;
            if (!Match.Teams.All(team => team.IsUnpauseMatch))
            {
                if (!askedForUnpause)
                    Server.PrintToChatAll(
                        Match.Plugin.Localizer[
                            "match.pause_unpause1",
                            Match.GetChatPrefix(),
                            player.Team.FormattedName,
                            player.Team.Opposition.FormattedName
                        ]
                    );
                return;
            }
            else
                Server.PrintToChatAll(
                    Match.Plugin.Localizer[
                        "match.pause_unpause2",
                        Match.GetChatPrefix(),
                        player.Team.FormattedName
                    ]
                );
            Server.ExecuteCommand("mp_unpause_match");
        }

        if (controller == null || AdminManager.PlayerHasPermissions(controller, "@css/config"))
        {
            Match.Log(
                printToChat: true,
                message: Match.Plugin.Localizer[
                    "match.admin_unpause",
                    Match.GetChatPrefix(true),
                    UtilitiesX.GetPlayerName(controller)
                ]
            );
            Server.ExecuteCommand("mp_unpause_match");
            return;
        }
    }
}
