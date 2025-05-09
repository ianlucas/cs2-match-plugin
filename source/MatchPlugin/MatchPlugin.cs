﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace MatchPlugin;

public partial class MatchPlugin : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "https://github.com/ianlucas/cs2-match-plugin";
    public override string ModuleName => "MatchPlugin";
    public override string ModuleVersion => "1.0.0";

    public static readonly List<string> LoadMatchCmds = ["match_load", "get5_loadmatch"];

    private readonly Match _match;

    public MatchPlugin()
    {
        _match = new(this);
        _match.bots.ValueChanged += OnMatchBotsChanged;
        _match.matchmaking.ValueChanged += OnMatchMatchmakingChanged;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        Extensions.ChangeTeamFunc.Hook(OnChangeTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        LoadMatchCmds.ForEach(c => AddCommand(c, "Load a match file.", OnMatchLoadCommand));
        AddCommand("match_status", "Print match status.", OnMatchStatusCommand);
        AddCommand("css_start", "Forcefully start match.", OnStartCommand);
        AddCommand("css_restart", "Forcefully restart match.", OnRestartCommand);
        AddCommand("css_map", "Change current map.", OnMapCommand);

        RegisterFakeConVars(_match);
        Directory.CreateDirectory(ServerX.GetConfigPath());
    }
}
