using Verse;

namespace Multiplayer.API;

/// <summary>
/// Signature for adding new local pause locking methods
/// </summary>
/// <param name="map">Current map to check if it should be paused</param>
/// <returns><see langword="true"/> if time should be paused on the specific map</returns>
public delegate bool PauseLockDelegate(Map map);