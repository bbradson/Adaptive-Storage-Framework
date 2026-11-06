namespace Multiplayer.API;

/// <summary>
/// Interface used by sessions that have restrictions based on other existing sessions, for example limiting them to only 1 session of specific type.
/// </summary>
public interface ISessionWithCreationRestrictions
{
    /// <summary>
    /// <para>Method used to check if the current session can be created by checking other <see cref="ISession"/>.</para>
    /// <para>Only sessions in the current context are checked (local map sessions or global sessions).</para>
    /// </summary>
    /// <param name="other">The other session the current one is checked against. Can be of different type.</param>
    /// <remarks>Currently only the current class checks against the existing ones - the existing classed don't check against this one.</remarks>
    /// <returns><see langword="true"/> if the current session should be created, <see langword="false"/> otherwise</returns>
    bool CanExistWith(Session other);
}