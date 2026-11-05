using Verse;

namespace Multiplayer.API;

/// <summary>
/// <para>Sessions implementing this interface consist of semi-persistent data.</para>
/// <para>Semi-persistent data:</para>
/// <list type="bullet">
///     <item>Serialized into binary using the Sync system</item>
///     <item>Session-bound: survives a reload, lost when the server is closed</item>
/// </list>
/// </summary>
public abstract class SemiPersistentSession : Session
{
    /// <inheritdoc cref="Session(Map)"/>
    protected SemiPersistentSession(Map map) : base(map) { }

    /// <summary>
    /// Writes/reads the data used by this session.
    /// </summary>
    /// <param name="sync">Sync worker used for writing/reading the data.</param>
    public abstract void Sync(SyncWorker sync);
}