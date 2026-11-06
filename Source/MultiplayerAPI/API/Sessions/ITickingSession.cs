namespace Multiplayer.API;

/// <summary>
/// Used by sessions that are are required to tick together with the map/world.
/// </summary>
public interface ITickingSession
{
    /// <summary>
    /// Called once per session when the map (for local sessions) or the world (for global sessions) is ticking.
    /// </summary>
    /// <remarks>The sessions are iterated over backwards using a for loop, so it's safe for them to remove themselves from the session manager.</remarks>
    void Tick();
}