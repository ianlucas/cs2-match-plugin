/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public partial class MatchPlugin
{
    private bool _pendingInternalPush = true;

    public void OnMapStart(string mapname)
    {
        _pendingInternalPush = true;
    }

    public void OnTick()
    {
        if (_pendingInternalPush)
        {
            _pendingInternalPush = false;
            OnConfigsExecuted();
        }
    }

    public void OnConfigsExecuted()
    {
        _match.SetState<WarmupState>();
    }
}
