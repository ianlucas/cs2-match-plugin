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

    private readonly Match _match = new();

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
    }
}
