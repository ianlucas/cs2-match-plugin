/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace MatchPlugin;

public partial class MatchPlugin
{
    private readonly Dictionary<string, Timer> _timers = [];
    private readonly float _chatInterval = 15.0f;

    public void ClearAllTimers()
    {
        foreach (var timer in _timers.Values)
            timer.Kill();
        _timers.Clear();
    }

    public void ClearTimer(string name)
    {
        if (_timers.TryGetValue(name, out var timer))
            timer.Kill();
        _timers.Remove(name);
    }

    public void CreateChatTimer(string name, Action callback)
    {
        _timers[name] = AddTimer(
            _chatInterval,
            callback,
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE
        );
    }
}
