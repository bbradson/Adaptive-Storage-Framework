using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// An exception that is thrown if you try to use the API without avaiable host.
/// </summary>
public class UninitializedAPI : Exception
{
}

class Dummy : IAPI
{
    public bool IsHosting => false;

    public bool IsInMultiplayer => false;

    public string PlayerName => null;

    public bool IsExecutingSyncCommand => false;

    public bool IsExecutingSyncCommandIssuedBySelf => false;

    public bool CanUseDevMode => false;

    public bool InInterface => false;
    public Faction RealPlayerFaction => null;

    public void SetThingFilterContext(ThingFilterContext context)
    {
        throw new UninitializedAPI();
    }

    public void WatchBegin()
    {
        throw new UninitializedAPI();
    }

    public void Watch(Type targetType, string fieldName, object target = null, object index = null)
    {
        throw new UninitializedAPI();
    }

    public void Watch(object target, string fieldName, object index = null)
    {
        throw new UninitializedAPI();
    }

    public void Watch(string memberPath, object target = null, object index = null)
    {
        throw new UninitializedAPI();
    }

    public void WatchEnd()
    {
        throw new UninitializedAPI();
    }

    public void RegisterAll(Assembly assembly)
    {
        throw new UninitializedAPI();
    }

    public ISyncField RegisterSyncField(Type targetType, string memberPath)
    {
        throw new UninitializedAPI();
    }

    public ISyncField RegisterSyncField(FieldInfo field)
    {
        throw new UninitializedAPI();
    }

    public ISyncMethod RegisterSyncMethod(Type type, string methodOrPropertyName, SyncType[] argTypes = null)
    {
        throw new UninitializedAPI();
    }

    public ISyncMethod RegisterSyncMethod(MethodInfo method, SyncType[] argTypes)
    {
        throw new UninitializedAPI();
    }

    public ISyncMethod RegisterSyncMethodLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal)
    {
        throw new UninitializedAPI();
    }

    public ISyncMethod RegisterSyncMethodLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal)
    {
        throw new UninitializedAPI();
    }

    public ISyncDelegate RegisterSyncDelegate(Type inType, string nestedType, string methodName, string[] fields, Type[] args = null)
    {
        throw new UninitializedAPI();
    }

    public ISyncDelegate RegisterSyncDelegate(Type type, string nestedType, string method)
    {
        throw new UninitializedAPI();
    }

    public ISyncDelegate RegisterSyncDelegateLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal)
    {
        throw new UninitializedAPI();
    }

    public ISyncDelegate RegisterSyncDelegateLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal)
    {
        throw new UninitializedAPI();
    }

    public ISyncDelegate RegisterSyncDelegateLocalFunc(Type parentType, string parentMethod, string localFuncName, Type[] parentArgs = null)
    {
        throw new UninitializedAPI();
    }

    public void RegisterSyncWorker<T>(SyncWorkerDelegate<T> syncWorkerDelegate, Type targetType = null, bool isImplicit = false, bool shouldConstruct = false)
    {
        throw new UninitializedAPI();
    }

    public void RegisterDialogNodeTree(Type type, string methodOrPropertyName, SyncType[] argTypes = null)
    {
        throw new UninitializedAPI();
    }

    public void RegisterDialogNodeTree(MethodInfo method)
    {
        throw new UninitializedAPI();
    }

    public void RegisterPauseLock(PauseLockDelegate pauseLock)
    {
        throw new UninitializedAPI();
    }

    public void RegisterDefaultLetterChoice(MethodInfo method, Type letterType = null)
    {
        throw new UninitializedAPI();
    }

    public Thing GetThingById(int id)
    {
        throw new UninitializedAPI();
    }

    public bool TryGetThingById(int id, out Thing value)
    {
        throw new UninitializedAPI();
    }

    public IReadOnlyList<IPlayerInfo> GetPlayers()
    {
        throw new UninitializedAPI();
    }

    public IPlayerInfo GetPlayerById(int id)
    {
        throw new UninitializedAPI();
    }

    public ISessionManager GetGlobalSessionManager()
    {
        throw new UninitializedAPI();
    }

    public ISessionManager GetLocalSessionManager(Map map)
    {
        throw new UninitializedAPI();
    }

    public void SetCurrentSessionWithTransferables(ISessionWithTransferables session)
    {
        throw new UninitializedAPI();
    }
}