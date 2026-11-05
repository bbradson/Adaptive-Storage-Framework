namespace Multiplayer.API;

/// <summary>
/// SyncWorker signature for adding new Types.
/// </summary>
/// <param name="obj">Target Type</param>
/// <remarks><see cref="SyncWorkerAttribute"/> for usage examples.</remarks>
public delegate void SyncWorkerDelegate<T>(SyncWorker sync, ref T obj);