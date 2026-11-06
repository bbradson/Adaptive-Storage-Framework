using System;

namespace Multiplayer.API;

/// <summary>
/// SyncMethod interface.
/// </summary>
/// <remarks>See <see cref="SyncMethodAttribute"/>, <see cref="MP.RegisterSyncMethod(System.Reflection.MethodInfo,Multiplayer.API.SyncType[])"/> and <see cref="MP.RegisterSyncMethod(Type, string, SyncType[])"/> to see how to use it.</remarks>
public interface ISyncMethod : ISyncCall
{
    /// <summary>
    /// Instructs SyncMethod to cancel synchronization if any arg is null.
    /// </summary>
    /// <returns>self</returns>
    ISyncMethod CancelIfAnyArgNull();

    /// <summary>
    /// Instructs SyncMethod to cancel synchronization if no map objects were selected during call replication.
    /// </summary>
    /// <returns>self</returns>
    ISyncMethod CancelIfNoSelectedMapObjects();

    /// <summary>
    /// Instructs SyncMethod to cancel synchronization if no world objects were selected during call replication.
    /// </summary>
    /// <returns>self</returns>
    ISyncMethod CancelIfNoSelectedWorldObjects();

    /// <summary>
    /// Use parameter's type's IExposable interface to transfer its data to other clients.
    /// </summary>
    /// <remarks>IExposable is the interface used for saving data to the save which means it utilizes IExposable.ExposeData() method.</remarks>
    /// <param name="index">Index at which parameter is to be marked to expose</param>
    /// <returns>self</returns>
    ISyncMethod ExposeParameter(int index);

    /// <summary>
    /// Currently unused in the Multiplayer mod.
    /// </summary>
    /// <param name="time">Milliseconds between resends</param>
    /// <returns>self</returns>
    ISyncMethod MinTime(int time);

    /// <summary>
    /// Instructs method to send context along with the call.
    /// </summary>
    /// <remarks>Context is restored after method is called.</remarks>
    /// <param name="context">One or more context flags</param>
    /// <returns>self</returns>
    ISyncMethod SetContext(SyncContext context);

    /// <summary>
    /// Instructs SyncMethod to synchronize only in debug mode.
    /// </summary>
    /// <returns>self</returns>
    ISyncMethod SetDebugOnly();

    /// <summary>
    /// Instructs SyncMethod to synchronize only if it's invoked by the host.
    /// </summary>
    /// <returns>self</returns>
    ISyncMethod SetHostOnly();

    /// <summary>
    /// Adds an Action that runs before a call is replicated on client.
    /// </summary>
    /// <param name="action">An action ran before a call is replicated on client. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncMethod SetPreInvoke(Action<object, object[]> action);

    /// <summary>
    /// Adds an Action that runs after a call is replicated on client.
    /// </summary>
    /// <param name="action">An action ran after a call is replicated on client. Called with target and value.</param>
    /// <returns>self</returns>
    ISyncMethod SetPostInvoke(Action<object, object[]> action);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version">Handler version</param>
    /// <returns>self</returns>
    ISyncMethod SetVersion(int version);

    /// <summary>
    /// Performs a transformation on a parameter of the synced method, the result of which will be synced instead of the parameter itself
    /// </summary>
    /// <param name="index">Index at which parameter is going to be transformed</param>
    /// <param name="serializer">A serializer which will transform the argument before and after syncing</param>
    /// <param name="skipTypeCheck">Check to ensure <typeparamref name="Live"/> is the same type as the argument will be dropped. More error-prone (and only detectable at runtime), but allows transforming arguments even if the current assembly cannot reference the specific type.</param>
    /// <typeparam name="Live">The type which will be transformed before, and type that <typeparamref name="Networked"/> will be transformed back into after syncing</typeparam>
    /// <typeparam name="Networked">The type which will be synced to other players instead of <typeparamref name="Live"/></typeparam>
    /// <returns>self</returns>
    ISyncMethod TransformArgument<Live, Networked>(int index, Serializer<Live, Networked> serializer, bool skipTypeCheck = false);

    /// <summary>
    /// Performs a transformation on the target instance of the synced method, the result of which will be synced instead of the instance itself
    /// </summary>
    /// <param name="serializer">A serializer which will transform the target instance before and after syncing</param>
    /// <param name="skipTypeCheck">Check to ensure <typeparamref name="Live"/> is the same type as the target instance will be dropped fully. More error-prone (and only detectable at runtime), but allows transforming target instance even if the current assembly cannot reference the specific type.</param>
    /// <typeparam name="Live">The type which will be transformed before, and type that <typeparamref name="Networked"/> will be transformed back into after syncing</typeparam>
    /// <typeparam name="Networked">The type which will be synced to other players instead of <typeparamref name="Live"/></typeparam>
    /// <returns>self</returns>
    ISyncMethod TransformTarget<Live, Networked>(Serializer<Live, Networked> serializer, bool skipTypeCheck = false);

    string ToString();
}