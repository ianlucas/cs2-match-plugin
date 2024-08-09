/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class State(Match match)
{
    public readonly Match Match = match;

    public virtual void Load() { }

    public virtual void Unload() { }
}
