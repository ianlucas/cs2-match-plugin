/*---------------------------------------------------------------------------------------------
*  Copyright (c) Ian Lucas. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchPlugin;

public class CSTV
{
    private readonly Match _match;
    private string? _filename;

    public CSTV(Match match)
    {
        _match = match;
        _match.Plugin.AddCommandListener("changelevel", OnChangeLevel);
    }

    public HookResult OnChangeLevel(CCSPlayerController? _, CommandInfo info)
    {
        if (IsRecording())
        {
            Stop();
            var arguments = info.ArgString.Trim().Replace("\"", "").ToLower();
            Server.ExecuteCommand($"changelevel \"{arguments}\"");
            return HookResult.Stop;
        }
        return HookResult.Continue;
    }

    public bool IsRecording() => _filename != null;

    public void Record(string? filename)
    {
        if (!IsActive() || IsRecording() || filename == null)
            return;
        _filename = filename;
        Server.ExecuteCommand($"tv_record {filename}");
    }

    public void Stop()
    {
        if (IsRecording())
        {
            _filename = null;
            Server.ExecuteCommand("tv_stoprecord");
        }
    }

    public string? GetFullPath() => _filename != null ? ServerX.GetFullPath(_filename) : null;

    public bool IsEnabled()
    {
        return ConVar.Find("tv_enable")?.GetPrimitiveValue<bool>() == true;
    }

    public bool IsActive()
    {
        return IsEnabled() && Utilities.GetPlayers().Any(p => p.IsHLTV);
    }

    public bool Set(bool value)
    {
        if (value)
        {
            if (!IsEnabled())
            {
                ConVar.Find("tv_enable")?.SetValue(true);
                ConVar.Find("tv_delay")?.SetValue(_match.tv_delay.Value);
            }
            if (!IsEnabled() || !IsActive())
            {
                Server.ExecuteCommand($"changelevel {Server.MapName}");
                return true;
            }
        }
        else if (IsEnabled() || IsActive())
        {
            ConVar.Find("tv_enable")?.SetValue(false);
            Server.ExecuteCommand($"changelevel {Server.MapName}");
            return true;
        }
        return false;
    }
}
