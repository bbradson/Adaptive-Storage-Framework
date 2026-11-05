using System;

namespace Multiplayer.API;

/// <summary>
/// Context flags which are sent along with a command
/// </summary>
[Flags]
public enum SyncContext
{
    /// <summary>Default value. (no context)</summary>
    None = 0,
    /// <summary>Send mouse cell context (emulates mouse position)</summary>
    MapMouseCell = 1,
    /// <summary>Send map selected context (object selected on the map)</summary>
    MapSelected = 2,
    /// <summary>Send world selected context (object selected on the world map)</summary>
    WorldSelected = 4,
    /// <summary>Send order queue context (emulates pressing KeyBindingDefOf.QueueOrder)</summary>
    QueueOrder_Down = 8,
    /// <summary>Send current map context</summary>
    CurrentMap = 16,
}