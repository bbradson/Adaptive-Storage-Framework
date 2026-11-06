namespace Multiplayer.API;

/// <summary>
/// ISyncCall interface.
/// </summary>
/// <remarks>Used internally</remarks>
public interface ISyncCall
{
    /// <summary>
    /// Manually calls the synced method.
    /// </summary>
    /// <param name="target">Object currently bound to that method. Null if the method is static.</param>
    /// <param name="args">Parameters to call the method with.</param>
    /// <returns><see langword="true"/> if the original call should be canceled.</returns>
    bool DoSync(object target, params object[] args);
}