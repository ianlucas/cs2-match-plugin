/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class Damage
{
    public int Value = 0;
    public int Hits = 0;
}

public class DamageReport(Player player)
{
    public Damage To = new();
    public Damage From = new();
    public Player Player = player;

    public void Reset()
    {
        To = new();
        From = new();
    }
}
