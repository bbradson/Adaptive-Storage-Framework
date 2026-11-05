using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Multiplayer.API;

public interface IAPI
{
    bool IsHosting { get; }
    bool IsInMultiplayer { get; }
    string PlayerName { get; }
    bool IsExecutingSyncCommand { get; }
    bool IsExecutingSyncCommandIssuedBySelf { get; }
    bool CanUseDevMode { get; }
    bool InInterface { get; }
    Faction RealPlayerFaction { get; }

    void SetThingFilterContext(ThingFilterContext context);

    void WatchBegin();
    void Watch(Type targetType, string fieldName, object target = null, object index = null);
    void Watch(object target, string fieldName, object index = null);
    void Watch(string memberPath, object target = null, object index = null);
    void WatchEnd();

    void RegisterAll(Assembly assembly);

    ISyncField RegisterSyncField(Type targetType, string memberPath);
    ISyncField RegisterSyncField(FieldInfo field);

    ISyncMethod RegisterSyncMethod(Type type, string methodOrPropertyName, SyncType[] argTypes = null);
    ISyncMethod RegisterSyncMethod(MethodInfo method, SyncType[] argTypes);
    ISyncMethod RegisterSyncMethodLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal);
    ISyncMethod RegisterSyncMethodLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal);

    ISyncDelegate RegisterSyncDelegate(Type type, string nestedType, string method);
    ISyncDelegate RegisterSyncDelegate(Type inType, string nestedType, string methodName, string[] fields, Type[] args = null);
    ISyncDelegate RegisterSyncDelegateLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal);
    ISyncDelegate RegisterSyncDelegateLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal);
    ISyncDelegate RegisterSyncDelegateLocalFunc(Type parentType, string parentMethod, string localFuncName, Type[] parentArgs = null);

    void RegisterSyncWorker<T>(SyncWorkerDelegate<T> syncWorkerDelegate, Type targetType = null, bool isImplicit = false, bool shouldConstruct = false);

    void RegisterDialogNodeTree(Type type, string methodOrPropertyName, SyncType[] argTypes = null);

    void RegisterDialogNodeTree(MethodInfo method);

    [Obsolete($"Use {nameof(Session)} instead.")]
    void RegisterPauseLock(PauseLockDelegate pauseLock);
        
    void RegisterDefaultLetterChoice(MethodInfo method, Type letterType = null);

    Thing GetThingById(int id);
    bool TryGetThingById(int id, out Thing value);

    IReadOnlyList<IPlayerInfo> GetPlayers();
    IPlayerInfo GetPlayerById(int id);

    ISessionManager GetGlobalSessionManager();
    ISessionManager GetLocalSessionManager(Map map);
    void SetCurrentSessionWithTransferables(ISessionWithTransferables session);
}