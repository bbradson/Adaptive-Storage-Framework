using System;

namespace Multiplayer.API;

/// <summary>
/// Sync delegate.
/// </summary>
/// <remarks>See <see cref="MP.RegisterSyncDelegate(Type, string, string)"/> and <see cref="MP.RegisterSyncDelegate(Type, string, string, string[], Type[])"/> to see how to use it.</remarks>
public interface ISyncDelegate : ISyncCall
{
    /// <summary>
    /// Instructs ISyncDelegate to cancel synchronization except for <param name="blacklist"/>
    /// </summary>
    /// <returns>self</returns>
    /// <param name="blacklist">field names to be excluded</param>
    ISyncDelegate CancelIfAnyFieldNull(params string[] blacklist);

    /// <summary>
    /// Instructs ISyncDelegate to cancel synchronization except for <param name="whitelist"/>
    /// </summary>
    /// <returns>self</returns>
    /// <param name="whitelist">Whitelist.</param>
    ISyncDelegate CancelIfFieldsNull(params string[] whitelist);

    /// <summary>
    /// Cancels if no selected objects.
    /// </summary>
    /// <returns>self</returns>
    [Obsolete($"Use {nameof(CancelIfNoSelectedMapObjects)} instead")]
    ISyncDelegate CancelIfNoSelectedObjects();

    /// <summary>
    /// Instructs SyncDelegate to cancel synchronization if no map objects were selected during call replication.
    /// </summary>
    /// <returns>self</returns>
    ISyncDelegate CancelIfNoSelectedMapObjects();

    /// <summary>
    /// Instructs SyncDelegate to cancel synchronization if no world objects were selected during call replication.
    /// </summary>
    /// <returns>self</returns>
    ISyncDelegate CancelIfNoSelectedWorldObjects();

    /// <summary>
    /// Use parameter's type's IExposable interface to transfer its data to other clients.
    /// </summary>
    /// <remarks>IExposable is the interface used for saving data to the save which means it utilizes IExposable.ExposeData() method.</remarks>
    /// <returns>self</returns>
    /// <param name="fields">Fields to sync by using IExposable.</param>
    ISyncDelegate ExposeFields(params string[] fields);

    /// <summary>
    /// Removes the nulls from lists.
    /// </summary>
    /// <returns>self</returns>
    /// <param name="listFields">List fields.</param>
    ISyncDelegate RemoveNullsFromLists(params string[] listFields);

    /// <summary>
    /// Sets the context.
    /// </summary>
    /// <returns>self</returns>
    /// <param name="context">Context.</param>
    ISyncDelegate SetContext(SyncContext context);

    /// <summary>
    /// Sets the debug only.
    /// </summary>
    /// <returns>self</returns>
    ISyncDelegate SetDebugOnly();

    /// <summary>
    /// Instructs SyncDelegate to synchronize only if it's invoked by the host.
    /// </summary>
    /// <returns>self</returns>
    ISyncDelegate SetHostOnly();

    /// <summary>
    /// Adds an Action that runs before a call is replicated on client.
    /// </summary>
    /// <param name="action">An action ran before a call is replicated on client. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncDelegate SetPreInvoke(Action<object, object[]> action);

    /// <summary>
    /// Adds an Action that runs after a call is replicated on client.
    /// </summary>
    /// <param name="action">An action ran after a call is replicated on client. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncDelegate SetPostInvoke(Action<object, object[]> action);

    /// <summary>
    /// Transforms a parameter of a method, result of which will be synced instead of the parameter itself
    /// </summary>
    /// <param name="index">Index at which parameter is going to be transformed</param>
    /// <param name="serializer">A serializer which will transform the argument before and after syncing</param>
    /// <param name="skipTypeCheck">Check to ensure <typeparamref name="Live"/> is the same type as the argument will be dropped. More error-prone (and only detectable at runtime), but allows transforming arguments even if the current assembly cannot reference the specific type.</param>
    /// <typeparam name="Live">The type which will be transformed before, and type that <typeparamref name="Networked"/> will be transformed back into after syncing</typeparam>
    /// <typeparam name="Networked">The type which will be synced to other players instead of <typeparamref name="Live"/></typeparam>
    /// <returns>self</returns>
    ISyncDelegate TransformArgument<Live, Networked>(int index, Serializer<Live, Networked> serializer, bool skipTypeCheck = false);

    /// <summary>
    /// Transforms an object instance within which the synced method is declared, result of which will be synced instead of the instance itself
    /// </summary>
    /// <param name="serializer">A serializer which will transform the target instance before and after syncing</param>
    /// <param name="skipTypeCheck">Check to ensure <typeparamref name="Live"/> is the same type as the target instance will be dropped. More error-prone (and only detectable at runtime), but allows transforming target instance even if the current assembly cannot reference the specific type.</param>
    /// <typeparam name="Live">The type which will be transformed before, and type that <typeparamref name="Networked"/> will be transformed back into after syncing</typeparam>
    /// <typeparam name="Networked">The type which will be synced to other players instead of <typeparamref name="Live"/></typeparam>
    /// <returns>self</returns>
    ISyncDelegate TransformTarget<Live, Networked>(Serializer<Live, Networked> serializer, bool skipTypeCheck = false);

    /// <summary>
    /// Transforms a field inside of the delegate, result of which will be synced instead of the field itself
    /// </summary>
    /// <param name="field">Name of a field which will be transformed before and after syncing. Supports fields inside of fields referencing other delegates, for example: `firstDelegate/anotherDelegate/targetField`</param>
    /// <param name="serializer">A serializer which will transform the field before and after syncing</param>
    /// <param name="skipTypeCheck">Check to ensure <typeparamref name="Live"/> is the same type as the field will be dropped. More error-prone (and only detectable at runtime), but allows transforming fields even if the current assembly cannot reference the specific type.</param>
    /// <typeparam name="Live">The type which will be transformed before, and type that <typeparamref name="Networked"/> will be transformed back into after syncing</typeparam>
    /// <typeparam name="Networked">The type which will be synced to other players instead of <typeparamref name="Live"/></typeparam>
    /// <returns></returns>
    ISyncDelegate TransformField<Live, Networked>(string field, Serializer<Live, Networked> serializer, bool skipTypeCheck = false);

    string ToString();
}