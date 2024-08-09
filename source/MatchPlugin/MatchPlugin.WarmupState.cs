/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class WarmupState(Match match) : State(match)
{
    public override void Load()
    {
        Config.ExecWarmup();
    }
}
