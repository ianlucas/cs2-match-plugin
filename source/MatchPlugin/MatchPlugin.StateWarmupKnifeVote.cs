/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class StateWarmupKnifeVote(Match match) : StateWarmup(match)
{
    public override void Load()
    {
        base.Load();

        Config.ExecWarmup(60);
    }
}
