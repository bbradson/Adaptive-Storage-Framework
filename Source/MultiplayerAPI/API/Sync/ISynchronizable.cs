namespace Multiplayer.API;

/// <summary>
/// An interface that allows syncing objects that inherit it.
/// </summary>
public interface ISynchronizable
{
    /// <summary>
    /// An entry point that is used when object is to be read/written.
    /// </summary>
    /// <remarks>
    /// <para>Requires a default constructor that takes no parameters.</para>
    /// <para>Check <see cref="SyncWorkerAttribute"/> to see how to make a syncer that allows for a manual object construction.</para>
    /// </remarks>
    /// <param name="sync">A SyncWorker that will read/write data bound with Bind methods.</param>
    /// <example>
    /// <para>A simple implementation that binds object's fields x, y, z for reading/writing.</para>
    /// <code>
    /// public void Sync(SyncWorker sync)
    ///    {
    ///        sync.Bind(ref this.x);
    ///        sync.Bind(ref this.y);
    ///        sync.Bind(ref this.z);
    ///    }
    /// </code>
    /// 
    /// <para>An implementation that sends field a, but saves it back into field b when it's received.</para>
    /// <code>
    /// public void Sync(SyncWorker sync)
    ///    {
    ///        if(sync.isWriting)
    ///            sync.Bind(ref this.a);
    ///        else
    ///            sync.Bind(ref this.b);
    ///    }
    /// </code>
    /// </example>
    void Sync(SyncWorker sync);
}