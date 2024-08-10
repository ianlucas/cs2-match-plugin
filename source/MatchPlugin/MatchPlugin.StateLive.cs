/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace MatchPlugin;

public class StateLive(Match match) : State(match)
{
    public override void Load()
    {
        Config.ExecLive(
            max_rounds: Match.max_rounds.Value,
            ot_max_rounds: Match.ot_max_rounds.Value
        );
    }
}
