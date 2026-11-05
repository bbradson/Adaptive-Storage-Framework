using System.Collections.Generic;
using Verse;

namespace Multiplayer.API;

public interface ISessionManager
{
    /// <summary>
    /// Returns the list of all currently active sessions for this specific <see cref="ISessionManager"/>.
    /// </summary>
    IReadOnlyList<Session> AllSessions { get; }
    /// <summary>
    /// Returns the list of all currently active exposable sessions for this specific <see cref="ISessionManager"/>.
    /// </summary>
    IReadOnlyList<ExposableSession> ExposableSessions { get; }
    /// <summary>
    /// Returns the list of all currently active semi-persistent sessions for this specific <see cref="ISessionManager"/>.
    /// </summary>
    IReadOnlyList<SemiPersistentSession> SemiPersistentSessions { get; }
    /// <summary>
    /// Returns the list of all currently active ticking sessions for this specific <see cref="ISessionManager"/>.
    /// </summary>
    IReadOnlyList<ITickingSession> TickingSessions { get; }
    /// <summary>
    /// A convenience property for checking if any of the sessions is active.
    /// </summary>
    bool AnySessionActive { get; }

    /// <summary>
    /// Adds a new session to the list of active sessions.
    /// </summary>
    /// <param name="session">The session to try to add to active sessions.</param>
    /// <returns><see langword="true"/> if the session was added to active ones, <see langword="false"/> if there was a conflict between sessions.</returns>
    bool AddSession(Session session);

    /// <summary>
    /// Tries to get a conflicting session (through the use of <see cref="ISessionWithCreationRestrictions"/>) or, if there was none, returns the input <paramref name="session"/>.
    /// </summary>
    /// <param name="session">The session to try to add to active sessions.</param>
    /// <returns>A session that was conflicting with the input one, or the input itself if there were no conflicts. It may be of a different type than the input.</returns>
    Session GetOrAddSessionAnyConflict(Session session);

    /// <summary>
    /// Tries to get a conflicting session (through the use of <see cref="ISessionWithCreationRestrictions"/>) or, if there was none, returns the input <paramref name="session"/>.
    /// </summary>
    /// <param name="session">The session to try to add to active sessions.</param>
    /// <returns>A session that was conflicting with the input one if it's the same type (<c>other is T</c>), null if it's a different type, or the input itself if there were no conflicts.</returns>
    T GetOrAddSession<T>(T session) where T : Session;

    /// <summary>
    /// Tries to remove a session from active ones.
    /// </summary>
    /// <param name="session">The session to try to remove from the active sessions.</param>
    /// <returns><see langword="true"/> if successfully removed from <see cref="AllSessions"/>. Doesn't correspond to if it was successfully removed from other lists of sessions.</returns>
    bool RemoveSession(Session session);

    /// <summary>
    /// Returns the first active session of specific type.
    /// </summary>
    /// <typeparam name="T">Type of the session to retrieve.</typeparam>
    /// <returns>The first session of specified type, or <see langword="null"/> if there are none.</returns>
    T GetFirstOfType<T>() where T : Session;

    /// <summary>
    /// Returns the session with specific ID of specific type.
    /// </summary>
    /// <param name="id">The ID of the session to search for.</param>
    /// <typeparam name="T">Type of the session to retrieve.</typeparam>
    /// <returns>The session with provided ID and of specified type, or <see langword="null"/> if there are none.</returns>
    T GetFirstWithId<T>(int id) where T : Session;

    /// <summary>
    /// Returns the session with specific ID.
    /// </summary>
    /// <param name="id">The ID of the session to search for.</param>
    /// <returns>The session with provided ID, or <see langword="null"/> if there are none.</returns>
    Session GetFirstWithId(int id);

    /// <summary>
    /// Checks if any of active sessions is currently pausing the game.
    /// </summary>
    /// <param name="map">The map at which the sessions would check if the game is paused. Global session manager accepts <see langword="null"/> for global pausing.</param>
    /// <returns><see langword="true"/> if any session is active, <see langword="false"/> otherwise.</returns>
    /// <remarks>Local session managers expect the <paramref name="map"/> to be the same as the map it's attached to.</remarks>
    bool IsAnySessionCurrentlyPausing(Map map); // Is it necessary for the API?
}