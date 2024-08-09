/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class Match
{
    private State State;

    public readonly List<Team> Teams = [];

    public Match()
    {
        var terrorists = new Team(CsTeam.Terrorist);
        var cts = new Team(CsTeam.CounterTerrorist);
        terrorists.Oppositon = cts;
        cts.Oppositon = cts;
        Teams = [terrorists, cts];
        State = new(this);
    }

    public void SetState<T>()
        where T : State
    {
        State =
            (T?)Activator.CreateInstance(typeof(T), this)
            ?? throw new InvalidOperationException("Failed to create instance of state.");
        ;
    }
}
