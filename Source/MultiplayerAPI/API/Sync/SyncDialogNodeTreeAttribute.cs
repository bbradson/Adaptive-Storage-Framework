using System;

namespace Multiplayer.API;

/// <summary>
/// An attribute that is used to mark methods which create <see cref="Verse.Dialog_NodeTree"/> for syncing.
/// Any option picked by a player will be synced between all clients automatically.
/// It can be combined with <see cref="SyncDialogNodeTreeAttribute"/> so the call will be replicated by the MPApi on all clients automatically.
/// </summary>
/// <example>
/// <para>An example showing how to mark a method for syncing.</para>
/// <code>
/// [SyncDialogNodeTree]
/// public void MyMethod(...)
/// {
///     ...
///     Find.WindowStack.Add(new Dialog_NodeTree(diaNode, faction));
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class SyncDialogNodeTreeAttribute : Attribute
{ }