using System;

namespace Multiplayer.API;

/// <summary>
/// An attribute that is used to mark fields for syncing.
/// It will be Watched for changes by the MPApi when instructed.
/// </summary>
/// <example>
/// <para>An example showing how to mark a field for syncing.</para>
/// <code>
/// [SyncField]
/// public class MyClass
/// {
///     [SyncField]
///     bool myField;
/// 
///     ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public class SyncFieldAttribute : Attribute
{
    public SyncContext context;

    /// <summary>Instructs SyncField to cancel synchronization if the value of the member it's pointing at is null.</summary>
    public bool cancelIfValueNull = false;

    /// <summary>Instructs SyncField to sync in game loop.</summary>
    public bool inGameLoop = false;

    /// <summary>Instructs SyncField to use a buffer instead of syncing instantly (when <see cref="MP.WatchEnd"/> is called).</summary>
    public bool bufferChanges = true;

    /// <summary>Instructs SyncField to synchronize only in debug mode.</summary>
    public bool debugOnly = false;

    /// <summary>Instructs SyncField to synchronize only if it's invoked by the host.</summary>
    public bool hostOnly = false;

    /// <summary></summary>
    public int version;

    /// <param name="context">Context</param>
    public SyncFieldAttribute(SyncContext context = SyncContext.None)
    {
        this.context = context;
    }
}