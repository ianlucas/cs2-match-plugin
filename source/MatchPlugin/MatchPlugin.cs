/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace MatchPlugin;

public partial class MatchPlugin : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "https://github.com/ianlucas/cs2-match-plugin";
    public override string ModuleName => "MatchPlugin";
    public override string ModuleVersion => "1.0.0";

    private readonly Match _match;

    public MatchPlugin()
    {
        _match = new(this);
        _match.bots.ValueChanged += OnMatchBotsChanged;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        Extensions.ChangeTeamFunc.Hook(OnChangeTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        AddCommand("match_status", "Print match status.", OnMatchStatusCommand);
        AddCommand("css_start", "Forcefully start match.", OnStartCommand);
    }
}
