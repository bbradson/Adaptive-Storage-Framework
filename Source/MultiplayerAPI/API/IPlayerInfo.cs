using System.Collections.Generic;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// An interface for the class holding player data
/// </summary>
public interface IPlayerInfo
{
    /// <summary>
    /// ID of the current player
    /// </summary>
    int Id { get; }
    /// <summary>
    /// Username of the current player
    /// </summary>
    string Username { get; }

    /// <summary>
    /// <see cref="Map.Index"/> of the map the player is on
    /// </summary>
    int CurrentMapIndex { get; }
    /// <summary>
    /// The map the current player is on
    /// </summary>
    Map CurrentMap { get; }

    /// <summary>
    /// List of all the things the player has selected, as numeric IDs
    /// </summary>
    /// <remarks>Generally use <see cref="SelectedThings"/>, unless you're able to access the IDs directly.</remarks>
    IReadOnlyList<int> SelectedThingsByIds { get; }
    /// <summary>
    /// List of all the things the player has selected
    /// </summary>
    IReadOnlyList<Thing> SelectedThings { get; }
}