namespace Multiplayer.API;

/// <summary>
/// Objects implementing this marker interface sync their exact type and all declared fields.
/// This is useful when syncing type hierarchies by value.
/// <remarks>The synced object is created uninitialized using reflection (no constructor is called).</remarks>
/// </summary>
public interface ISyncSimple { }