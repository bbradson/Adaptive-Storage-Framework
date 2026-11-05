using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// The primary static class that contains methods used to interface with the multiplayer mod.
/// </summary>
[StaticConstructorOnStartup]
public static class MP
{
    /// <summary>Contains the API version</summary>
    public const string API = "0.6";

    /// <value>
    /// Returns <see langword="true"/> if API is initialized.
    /// </value>
    public static readonly bool enabled = false;

    private static readonly IAPI Sync;

    static MP()
    {
        var mpAssembly = LoadedModManager.RunningMods
            .SelectMany(m => m.assemblies.loadedAssemblies)
            .FirstOrDefault(a => a.GetName().Name == "Multiplayer");

        if (mpAssembly == null)
        {
            Sync = new Dummy();

            return;
        }

        // This can fail in older MP versions halting mod loading
        try {
            Sync = (IAPI) mpAssembly
                .GetType("Multiplayer.Common.MultiplayerAPIBridge")
                .GetField("Instance")
                .GetValue(null);

            enabled = true;
        } catch(Exception e) {
            Log.Error("Multiplayer mod detected but it has no MPAPI Bridge\n\n" + e);
        }
    }

    // Some nice shortcuts ready to be inlined by the JIT compiler :)

    /// <value>
    /// Returns <see langword="true"/> if currently running on a host.
    /// </value>
    public static bool IsHosting => Sync.IsHosting;

    /// <value>
    /// Returns <see langword="true"/> if currently running in a multiplayer session (both on client and host).
    /// </value>
    public static bool IsInMultiplayer => Sync.IsInMultiplayer;

    /// <value>
    /// Returns local player's name.
    /// </value>
    public static string PlayerName => Sync.PlayerName;

    /// <value>
    /// Returns <see langword="true"/> if currently there's a sync command being executed.
    /// </value>
    public static bool IsExecutingSyncCommand => Sync.IsExecutingSyncCommand;

    /// <value>
    /// Returns <see langword="true"/> if currently there's a sync command being executed that was issued by the current player.
    /// </value>
    public static bool IsExecutingSyncCommandIssuedBySelf => Sync.IsExecutingSyncCommandIssuedBySelf;

    /// <summary>
    /// Returns <see langword="true"/> if the current player is allowed to use dev mode commands.
    /// </summary>
    public static bool CanUseDevMode => Sync.CanUseDevMode;

    /// <summary>
    /// Returns <see langword="true"/> roughly when not executing code which is running deterministically on all clients.
    /// Such code needs to use the Sync system to synchronize game state changes and a method can detect whether it needs to synchronize using this property.
    /// It's <see langword="true"/>, for example, in <c>OnGUI</c> (hence the name).
    /// </summary>
    /// <value>
    /// Returns <see langword="true"/> if all the following conditions are <see langword="true"/>:
    /// <list type="bullet">
    ///     <item><description>Multiplayer mod is enabled</description></item>
    ///     <item><description>The game is currently in multiplayer mode</description></item>
    ///     <item><description>The game is currently not ticking</description></item>
    ///     <item><description>The game is not running sync commands</description></item>
    ///     <item><description>The multiplayer game is not being reloaded</description></item>
    ///     <item><description><see cref="Current.ProgramState"/> is <see cref="ProgramState.Playing"/></description></item>
    ///     <item><description><see cref="LongEventHandler.currentEvent"/> is <see langword="null"/></description></item>
    /// </list>
    /// If any of them are <see langword="false"/>, it returns <see langword="false"/>.
    /// </value>
    public static bool InInterface => Sync.InInterface;
    
    /// <summary>
    /// The actual faction of the player irrespective of the faction context which might be set in multifaction.
    /// In singleplayer, returns <c>Faction.OfPlayer</c>.
    /// </summary>
    public static Faction RealPlayerFaction => Sync.RealPlayerFaction;

    /// <summary>
    /// Used to set the ThingFilter context for interactions with ThingFilter UI.
    /// Set the context before drawing the ThingFilter and then set it back to <see langword="null"/> after it's drawn.
    /// <remarks>
    /// This method is not "reentrant". If you call it twice without setting the context back to <see langword="null"/>, the second call will throw an exception.
    /// </remarks>
    /// </summary>
    /// <param name="context">The ThingFilter context object</param>
    public static void SetThingFilterContext(ThingFilterContext context) => Sync.SetThingFilterContext(context);

    /// <summary>
    /// Starts a new synchronization stack.
    /// </summary>
    /// <remarks>
    /// <para>Has to be called before invoking Watch methods.</para>
    /// <para>See also <see cref="ISyncField.Watch(object, object)"/>.</para>
    /// </remarks>
    public static void WatchBegin() => Sync.WatchBegin();

    /// <summary>
    /// Helper method for <see cref="ISyncField.Watch"/> given a type.
    /// </summary>
    /// <param name="type">An object of type set in the <see cref="ISyncField"/> to watch, for static types</param>
    /// <param name="fieldName"><see cref="ISyncField"/> name of the field to watch for changes</param>
    /// <param name="index">Index in the field path set in <see cref="ISyncField"/></param>
    public static void Watch(Type type, string fieldName, object index = null) => Sync.Watch(type, fieldName, index);

    /// <summary>
    /// Helper method for <see cref="ISyncField.Watch"/> given an instance.
    /// </summary>
    /// <param name="target">An object of type set in the <see cref="ISyncField"/> to watch</param>
    /// <param name="fieldName"><see cref="ISyncField"/> name of the field to watch for changes</param>
    /// <param name="index">Index in the field path set in <see cref="ISyncField"/></param>
    public static void Watch(object target, string fieldName, object index = null) => Sync.Watch(target, fieldName, index);

    /// <summary>
    /// Helper method for <see cref="ISyncField.Watch"/> given an instance.
    /// </summary>
    /// <param name="memberPath"><see cref="ISyncField"/> the memberPath of the ISyncField</param>
    /// <param name="target">An object of type set in the <see cref="ISyncField"/> to watch, null for static</param>
    /// <param name="index">Index in the field path set in <see cref="ISyncField"/></param>
    public static void Watch(string memberPath, object target = null, object index = null) => Sync.Watch(memberPath, target, index);

    /// <summary>
    /// Ends the current synchronization stack and executes it.
    /// </summary>
    /// <remarks>
    /// <para>Has to be called after invoking Watch methods.</para>
    /// <para>See also <see cref="ISyncField.Watch(object, object)"/>.</para>
    /// </remarks>
    public static void WatchEnd() => Sync.WatchEnd();

    /// <summary>
    /// Searches current assembly for MPAPI annotations and registers them
    /// <see cref="SyncMethodAttribute"/>
    /// <see cref="SyncFieldAttribute"/>
    /// </summary>
    public static void RegisterAll() => RegisterAll(new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly);

    /// <summary>
    /// Searches the given assembly for MPAPI annotations and registers them
    /// <param name="assembly">The assembly</param>
    /// <see cref="SyncMethodAttribute"/>
    /// <see cref="SyncFieldAttribute"/>
    /// </summary>
    public static void RegisterAll(Assembly assembly) => Sync.RegisterAll(assembly);

    /// <summary>
    /// Registers a field for syncing and returns it's <see cref="ISyncField"/>.
    /// </summary>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncFieldAttribute"/> instead, unless you have to otherwise.</para>
    /// <para>They must be Watched between MP.WatchBegin and MP.WatchEnd with the MP.Watch* methods</para>
    /// </remarks>
    /// <param name="targetType">
    /// <para>Type of the target class that contains the specified member</para>
    /// <para>if null, <paramref name="memberPath"/> will point at field from the global namespace in the "Type/fieldName" format.</para>
    /// </param>
    /// <param name="memberPath">Path to a member. If the member is to be indexed, it has to end with /[] eg. <c>"myArray/[]"</c></param>
    /// <returns>A new registered <see cref="ISyncField"/></returns>
    public static ISyncField RegisterSyncField(Type targetType, string memberPath) => Sync.RegisterSyncField(targetType, memberPath);

    /// <summary>
    /// Registers a field for syncing and returns it's <see cref="ISyncField"/>.
    /// </summary>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncFieldAttribute"/> instead, unless you have to otherwise.</para>
    /// <para>They must be Watched between MP.WatchBegin and MP.WatchEnd with the MP.Watch* methods</para>
    /// </remarks>
    /// <param name="field">FieldInfo of a field to register</param>
    /// <returns>A new registered <see cref="ISyncField"/></returns>
    public static ISyncField RegisterSyncField(FieldInfo field) => Sync.RegisterSyncField(field);

    /// <summary>
    /// Registers a method for syncing and returns its <see cref="ISyncMethod"/>.
    /// </summary>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncMethodAttribute"/> instead, unless you have to otherwise.</para>
    /// </remarks>
    /// <param name="type">Type that contains the method</param>
    /// <param name="methodOrPropertyName">Name of the method</param>
    /// <param name="argTypes">Method's parameter types</param>
    /// <returns>A new registered <see cref="ISyncMethod"/></returns>
    public static ISyncMethod RegisterSyncMethod(Type type, string methodOrPropertyName, SyncType[] argTypes = null) => Sync.RegisterSyncMethod(type, methodOrPropertyName, argTypes);

    /// <summary>
    /// Registers a method for syncing and returns its <see cref="ISyncMethod"/>.
    /// </summary>
    /// <param name="method">MethodInfo of a method to register</param>
    /// <param name="argTypes">Method's parameter types</param>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncMethodAttribute"/> instead, unless you have to otherwise.</para>
    /// </remarks>
    /// <returns>A new registered <see cref="ISyncMethod"/></returns>
    /// <example>
    /// Register a method for syncing using reflection and set it to debug only.
    /// <code>
    ///    RegisterSyncMethod(typeof(MyType).GetMethod(nameof(MyType.MyMethod))).SetDebugOnly();
    /// </code>
    /// </example>
    public static ISyncMethod RegisterSyncMethod(MethodInfo method, SyncType[] argTypes = null) => Sync.RegisterSyncMethod(method, argTypes);

    /// <summary>
    /// Registers a compiler-generated lambda for syncing and returns its <see cref="ISyncMethod"/>, you will have to figure out the ordinal of your target by decompiling.
    /// </summary>
    /// <param name="parentType">Type that contains the method.</param>
    /// <param name="parentMethod">Name of the method the lambda is a child of.</param>
    /// <param name="lambdaOrdinal">For example, with lambdaOrdinal = 3: &lt;FillTab&gt;b__10_3</param>
    /// <param name="parentArgs">Arguments of the parent method. Needed if there's an more than 1 method with the same name.</param>
    /// <param name="parentParentMethodType">The type of the parent method.</param>
    /// <returns>A new registered <see cref="ISyncMethod"/></returns>
    public static ISyncMethod RegisterSyncMethodLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal) 
        => Sync.RegisterSyncMethodLambda(parentType, parentMethod, lambdaOrdinal, parentArgs, parentParentMethodType);

    /// <summary>
    /// Registers a compiler-generated lambda for syncing and returns its <see cref="ISyncMethod"/>, you will have to figure out the ordinal of your target by decompiling.
    /// <remarks>
    /// <para>Exists for convenience, the outcome will be the same as calling <see href="RegisterSyncMethodLambda"/> with parentMethod set to <see cref="ParentMethodType.Getter"/></para>
    /// </remarks>
    /// </summary>
    /// <param name="parentType">Type that contains the method.</param>
    /// <param name="parentMethod">Name of the method the lambda is a child of.</param>
    /// <param name="lambdaOrdinal">For example, with lambdaOrdinal = 3: &lt;FillTab&gt;b__10_3</param>
    /// <returns>A new registered <see cref="ISyncMethod"/></returns>
    public static ISyncMethod RegisterSyncMethodLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal)
        => Sync.RegisterSyncMethodLambdaInGetter(parentType, parentMethod, lambdaOrdinal);

    /// <summary>
    /// Registers the syncDelegate. Handles anonymous nested types, you will have to figure out the name of your target by decompiling.
    /// </summary>
    /// <returns>The sync delegate.</returns>
    /// <param name="type">Type.</param>
    /// <param name="nestedType">Nested type.</param>
    /// <param name="method">Method.</param>
    public static ISyncDelegate RegisterSyncDelegate(Type type, string nestedType, string method) => Sync.RegisterSyncDelegate(type, nestedType, method);

    /// <summary>
    /// Registers the syncDelegate. Handles anonymous nested types, you will have to figure out the name of your target by decompiling.
    /// </summary>
    /// <returns>The sync delegate.</returns>
    /// <param name="inType">In type.</param>
    /// <param name="nestedType">Nested type.</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="fields">Fields.</param>
    /// <param name="args">Arguments.</param>
    public static ISyncDelegate RegisterSyncDelegate(Type inType, string nestedType, string methodName, string[] fields, Type[] args = null) => Sync.RegisterSyncDelegate(inType, nestedType, methodName, fields, args);
        
    /// <summary>
    /// Registers the syncDelegate. Handles anonymous nested types, you will have to figure out the name and lambda ordinal of your target by decompiling.
    /// </summary>
    /// <returns>The sync delegate.</returns>
    /// <param name="parentType">Type that contains the method.</param>
    /// <param name="parentMethod">Name of the method the lambda is a child of.</param>
    /// <param name="lambdaOrdinal">For example, with lambdaOrdinal = 3: &lt;FillTab&gt;b__10_3</param>
    /// <param name="parentArgs">Arguments of the parent method. Needed if there's an more than 1 method with the same name.</param>
    /// <param name="parentParentMethodType">The type of the parent method.</param>
    public static ISyncDelegate RegisterSyncDelegateLambda(Type parentType, string parentMethod, int lambdaOrdinal, Type[] parentArgs = null, ParentMethodType parentParentMethodType = ParentMethodType.Normal)
        => Sync.RegisterSyncDelegateLambda(parentType, parentMethod, lambdaOrdinal, parentArgs, parentParentMethodType);

    /// <summary>
    /// Registers the syncDelegate. Handles anonymous nested types, you will have to figure out the name and lambda ordinal of your target by decompiling.
    /// </summary>
    /// <returns>The sync delegate.</returns>
    /// <param name="parentType">Type that contains the method.</param>
    /// <param name="parentMethod">Name of the method the lambda is a child of.</param>
    /// <param name="lambdaOrdinal">For example, with lambdaOrdinal = 3: &lt;FillTab&gt;b__10_3</param>
    public static ISyncDelegate RegisterSyncDelegateLambdaInGetter(Type parentType, string parentMethod, int lambdaOrdinal)
        => Sync.RegisterSyncDelegateLambdaInGetter(parentType, parentMethod, lambdaOrdinal);

    /// <summary>
    /// Registers the syncDelegate. Handles anonymous nested types, you will have to figure out the name and lambda ordinal of your target by decompiling.
    /// </summary>
    /// <returns>The sync delegate.</returns>
    /// <param name="parentType">Type that contains the method.</param>
    /// <param name="parentMethod">Name of the method the lambda is a child of.</param>
    /// <param name="localFuncName">For example, for local function named Start: &lt;DoWindowContents&gt;g__Start|10</param>
    /// <param name="parentArgs">Arguments of the parent method. Needed if there's an more than 1 method with the same name.</param>
    public static ISyncDelegate RegisterSyncDelegateLocalFunc(Type parentType, string parentMethod, string localFuncName, Type[] parentArgs = null)
        => Sync.RegisterSyncDelegateLocalFunc(parentType, parentMethod, localFuncName, parentArgs);

    /// <summary>
    /// Registers the SyncWorker based on SyncWorkerDelegate.
    /// </summary>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncWorkerAttribute"/> instead, unless you have to otherwise.</para>
    /// </remarks>
    /// <param name="syncWorkerDelegate">Sync worker delegate.</param>
    /// <param name="targetType">Type to handle.</param>
    /// <param name="isImplicit">If set to <c>true</c> the SyncWorker will handle the type and all the derivate Types.</param>
    /// <param name="shouldConstruct">If set to <c>true</c> the SyncWorker will be provided with an instance created with no arguments.</param>
    /// <typeparam name="T">Type to handle.</typeparam>
    public static void RegisterSyncWorker<T>(SyncWorkerDelegate<T> syncWorkerDelegate, Type targetType = null, bool isImplicit = false, bool shouldConstruct = false) => Sync.RegisterSyncWorker(syncWorkerDelegate, targetType, isImplicit: isImplicit, shouldConstruct: shouldConstruct);

    /// <summary>
    /// Registers a method which opens a <see cref="Dialog_NodeTree"/>. The options picked by players will then be synced between all clients.
    /// </summary>
    /// <param name="type">Type that contains the method</param>
    /// <param name="methodOrPropertyName">Name of the method</param>
    /// <param name="argTypes">Method's parameter types</param>
    public static void RegisterSyncDialogNodeTree(Type type, string methodOrPropertyName, SyncType[] argTypes = null) => Sync.RegisterDialogNodeTree(type, methodOrPropertyName, argTypes);

    /// <summary>
    /// Registers a method which opens a <see cref="Dialog_NodeTree"/>. The options picked by players will then be synced between all clients.
    /// </summary>
    /// <param name="method">MethodInfo of a method to register</param>
    /// <remarks>
    /// <para>It's recommended to use <see cref="SyncDialogNodeTreeAttribute"/> instead, unless you have to otherwise.</para>
    /// <para>It can be combined with <see cref="RegisterSyncMethod"/> so the call will be replicated by the MPApi on all clients automatically.</para>
    /// </remarks>
    /// <example>
    /// Register a method creating a <see cref="Dialog_NodeTree"/> for syncing using reflection and set it to debug only.
    /// <code>
    ///    RegisterSyncDialogNodeTree(typeof(MyType).GetMethod(nameof(MyType.MyMethod))).SetDebugOnly();
    /// </code>
    /// </example>
    public static void RegisterSyncDialogNodeTree(MethodInfo method) => Sync.RegisterDialogNodeTree(method);

    /// <summary>
    /// Registers a delegate which will be called to check if the game should be paused on specific map.
    /// In case async time is active, only that map will be paused, otherwise all of them will be paused.
    /// If async time is enabled, it'll also be called globally with <see langword="null"/> as the parameter, which will affect all maps. It's skipped if async time is disabled.
    /// </summary>
    /// <param name="pauseLock">Delegate with <see cref="Map"/> as the only parameter returning a <see cref="bool"/> which will be called to see if the game should be paused</param>
    /// <remarks>It's recommended to use <see cref="PauseLockAttribute"/> instead, unless you have to otherwise.</remarks>
    /// <example>
    /// Register a method as a delagate for forced pause locking
    /// <code>
    ///     void Register() => RegisterPauseLock(MyMethod);
    ///     void MyMethod(Map map) => return MyOtherClass.shouldPause;
    /// </code>
    ///
    /// Register a dynamic method as a delegate for forced pause locking
    /// <code>
    ///     RegisterPauseLock(map => MyOtherClass.shouldPause);
    /// </code>
    /// </example>
    [Obsolete($"Use {nameof(Session)} instead.")]
    public static void RegisterPauseLock(PauseLockDelegate pauseLock) => Sync.RegisterPauseLock(pauseLock);

    /// <summary>
    /// In multiplayer, choice letters don't pause the game when expiring - instead, using a default choice (usually rejecting, if applicable).
    /// This does not automatically sync choices, and the choices themselves need syncing through a sync method/delegate.
    /// </summary>
    /// <param name="method">Method that will be called when the letter expires. Can either be a method inside of the letter class itself, or a static method (with the instance as the parameter).</param>
    /// <param name="letterType">The type of the letter. If null, <see cref="MethodInfo.DeclaringType"/> will be used.</param>
    public static void RegisterDefaultLetterChoice(MethodInfo method, Type letterType = null) => Sync.RegisterDefaultLetterChoice(method, letterType);

    /// <summary>
    /// Retrieves a <see cref="Thing"/> with a provided id <see cref="Thing.thingIDNumber"/>
    /// </summary>
    /// <param name="id"><see cref="Thing.thingIDNumber"/> for the <see cref="Thing"/> to retrieve</param>
    /// <returns><see cref="Thing"/> with a specific numeric ID.</returns>
    public static Thing GetThingById(int id) => Sync.GetThingById(id);
    /// <summary>
    /// Retrieves a <see cref="Thing"/> with a provided id <see cref="Thing.thingIDNumber"/> and returns a <see cref="bool"/> for success/failure
    /// </summary>
    /// <param name="id"><see cref="Thing.thingIDNumber"/> for the <see cref="Thing"/> to retrieve</param>
    /// <param name="value">The value of retrieved <see cref="Thing"/>, if any.</param>
    /// <returns><see langword="true"/> if successful</returns>
    public static bool TryGetThingById(int id, out Thing value) => Sync.TryGetThingById(id, out value);

    /// <summary>
    /// Retrieves a list of <see cref="IPlayerInfo"/> for every player
    /// </summary>
    /// <returns>List with every <see cref="IPlayerInfo"/></returns>
    public static IReadOnlyList<IPlayerInfo> GetPlayers() => Sync.GetPlayers();
    /// <summary>
    /// Retrieves a <see cref="IPlayerInfo"/> with a specific <see cref="IPlayerInfo.Id"/>
    /// </summary>
    /// <param name="id"><see cref="IPlayerInfo.Id"/> of the player to retrieve</param>
    /// <returns>Player with specified ID number</returns>
    public static IPlayerInfo GetPlayerById(int id) => Sync.GetPlayerById(id);

    /// <summary>
    /// Retrieves the global (world) session manager.
    /// </summary>
    /// <returns>The global (world) session manager.</returns>
    /// <remarks>As long as a multiplayer session is active, there should always be a global session manager. This method should never return <see langword="null"/> in such cases, unless something is very broken.</remarks>
    public static ISessionManager GetGlobalSessionManager() => Sync.GetGlobalSessionManager();
    /// <summary>
    /// Retrieves the local (map) session manager.
    /// </summary>
    /// <param name="map">The map whose session manager will be retrieved.</param>
    /// <returns>The local (map) session manager.</returns>
    /// <remarks>As long as a multiplayer session is active, all maps should contain a session manager. This method should never return <see langword="null"/> in such cases, unless something is very broken.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is null.</exception>
    public static ISessionManager GetLocalSessionManager(Map map) => Sync.GetLocalSessionManager(map);
    /// <summary>
    /// <para>Sets the currently active session with transferables. Used for syncing changes in transferables by letting Multiplayer know which session should be synced.</para>
    /// <para>It cannot be set to anything but <see langword="null"/> while a session is currently set as active.</para>
    /// <para>The session needs to be set before (potentially) operating on the trasnferables, and unset afterwards.</para>
    /// <para>The session should be set/unset in a <see langword="try"/><see langword="finally"/> block.</para>
    /// </summary>
    /// <param name="session">The session to set as the active one, or <see langword="null"/> to unset.</param>
    /// <example>
    /// <code>
    ///     try
    ///     {
    ///         MP.SetCurrentSessionWithTransferables(session);
    ///         OperateOnTransferables();
    ///     }
    ///     finally
    ///     {
    ///         MP.SetCurrentSessionWithTransferables(null);
    ///     }
    /// </code>
    /// </example>
    public static void SetCurrentSessionWithTransferables(ISessionWithTransferables session) => Sync.SetCurrentSessionWithTransferables(session);
}