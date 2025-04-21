/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;

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
        if (!IsEnabled() || IsRecording() || filename == null)
            return;
        _filename = filename;
        _match.Log($"Demo is being recorded at {filename}.");
        Server.ExecuteCommand($"tv_record {filename}");
    }

    public void Stop()
    {
        if (IsRecording())
        {
            _filename = null;
            _match.Log($"Demo is no longer being recorded.");
            Server.ExecuteCommand("tv_stoprecord");
        }
    }

    public string? GetFilename() => _filename;

    public bool IsEnabled() => ConVar.Find("tv_enable")?.GetPrimitiveValue<bool>() == true;

    public void Set(bool value)
    {
        if (value)
        {
            ConVar.Find("tv_enable")?.SetValue(true);
            ConVar.Find("tv_record_immediate")?.SetValue(1);
            ConVar.Find("tv_delay")?.SetValue(_match.tv_delay.Value);
        }
        else
            ConVar.Find("tv_enable")?.SetValue(false);
    }
}
