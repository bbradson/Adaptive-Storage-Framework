using System;

namespace Multiplayer.API;

/// <summary>
/// SyncField interface.
/// </summary>
/// <example>
/// <para>Creates and registers a SyncField that points to <c>myField</c> in object of type <c>MyType</c> and enables its change buffer.</para>
/// <code>
/// MPApi.SyncField(typeof(MyType), "myField").SetBufferChanges();
/// </code>
/// <para>Creates and registers a SyncField that points to <c>myField</c> which resides in <c>MyStaticClass</c>.</para>
/// <code>
/// MPApi.SyncField(null, "MyAssemblyNamespace.MyStaticClass.myField");
/// </code>
/// <para>Creates and registers a SyncField that points to <c>myField</c> that resides in an object stored by myEnumberable defined in an object of type <c>MyType</c>.</para>
/// <para>To watch this one you have to supply an index in <see cref="Watch(object, object)"/>.</para>
/// <code>
/// MPApi.SyncField(typeof(MyType), "myEnumerable/[]/myField");
/// </code>
/// </example>
public interface ISyncField
{
    /// <summary>
    /// Instructs SyncField to cancel synchronization if the value of the member it's pointing at is null.
    /// </summary>
    /// <returns>self</returns>
    ISyncField CancelIfValueNull();

    /// <summary>
    /// Instructs SyncField to sync in game loop.
    /// </summary>
    /// <returns>self</returns>
    ISyncField InGameLoop();

    /// <summary>
    /// Adds an Action that runs after a field is synchronized.
    /// </summary>
    /// <param name="action">An action ran after a field is synchronized. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncField PostApply(Action<object, object> action);

    /// <summary>
    /// Adds an Action that runs before a field is synchronized.
    /// </summary>
    /// <param name="action">An action ran before a field is synchronized. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncField PreApply(Action<object, object> action);

    /// <summary>
    /// Instructs SyncField to use a buffer instead of syncing instantly (when <see cref="MP.WatchEnd"/> is called).
    /// </summary>
    /// <returns>self</returns>
    ISyncField SetBufferChanges();

    /// <summary>
    /// Instructs SyncField to synchronize only in debug mode.
    /// </summary>
    /// <returns>self</returns>
    ISyncField SetDebugOnly();

    /// <summary>
    /// Instructs SyncField to synchronize only if it's invoked by the host.
    /// </summary>
    /// <returns>self</returns>
    ISyncField SetHostOnly();

    /// <summary>
    /// 
    /// </summary>
    /// <returns>self</returns>
    ISyncField SetVersion(int version);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target">An object of type set in the <see cref="ISyncField"/>. Set to null if you're watching a static field.</param>
    /// <param name="index">Index in the field path set in <see cref="ISyncField"/>.</param>
    /// <returns>self</returns>
    void Watch(object target = null, object index = null);

    /// <summary>
    /// Manually syncs a field.
    /// </summary>
    /// <param name="target">An object of type set in the <see cref="ISyncField"/>. Set to null if you're watching a static field.</param>
    /// <param name="value">Value to apply to the synced field.</param>
    /// <param name="index">Index in the field path set in <see cref="ISyncField"/></param>
    /// <returns><see langword="true"/> if the change should be canceled.</returns>
    bool DoSync(object target, object value, object index = null);

    string ToString();
}