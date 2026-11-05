using System;

namespace Multiplayer.API;

/// <summary>
/// An attribute that is used to mark methods for syncing.
/// The call will be replicated by the MPApi on all clients automatically.
/// </summary>
/// <example>
/// <para>An example showing how to mark a method for syncing.</para>
/// <code>
/// [SyncMethod]
/// public void MyMethod(...)
/// {
///     ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class SyncMethodAttribute : Attribute
{
    public SyncContext context;

    /// <summary>Instructs SyncMethod to cancel synchronization if any arg is null (see <see cref="ISyncMethod.CancelIfAnyArgNull"/>).</summary>
    public bool cancelIfAnyArgNull = false;

    /// <summary>Instructs SyncMethod to cancel synchronization if no map objects were selected during the call (see <see cref="ISyncMethod.CancelIfNoSelectedMapObjects"/>).</summary>
    public bool cancelIfNoSelectedMapObjects = false;

    /// <summary>Instructs SyncMethod to cancel synchronization if no world objects were selected during call replication(see <see cref="ISyncMethod.CancelIfNoSelectedWorldObjects"/>).</summary>
    public bool cancelIfNoSelectedWorldObjects = false;

    /// <summary>Instructs SyncMethod to synchronize only in debug mode (see <see cref="ISyncMethod.SetDebugOnly"/>).</summary>
    public bool debugOnly = false;

    /// <summary>A list of types to expose (see <see cref="ISyncMethod.ExposeParameter"/>)</summary>
    public int[] exposeParameters;

    /// <param name="context">Context</param>
    public SyncMethodAttribute(SyncContext context = SyncContext.None)
    {
        this.context = context;
    }
}