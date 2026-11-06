using RimWorld;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// <para>Required by sessions dealing with transferables, like trading or caravan forming. By implementing this interface, Multiplayer will handle majority of syncing of changes in transferables.</para>
/// <para>When drawing the dialog tied to this session, you'll have to set <see cref="MP.SetCurrentSessionWithTransferables"/> to the proper session, and set it to null once done.</para>
/// </summary>
/// <remarks>For safety, make sure to set <see cref="MP.SetCurrentSessionWithTransferables"/> in <see langword="try"/> and unset in <see langword="finally"/>.</remarks>
public interface ISessionWithTransferables
{
    /// <summary>
    /// Used when syncing data across players, specifically to retrieve <see cref="Transferable"/> based on the <see cref="Thing"/> it has.
    /// </summary>
    /// <param name="thingId"><see cref="Thing.thingIDNumber"/> of the <see cref="Thing"/>.</param>
    /// <returns><see cref="Transferable"/> which corresponds to a <see cref="Thing"/> with specific <see cref="Thing.thingIDNumber"/>.</returns>
    Transferable GetTransferableByThingId(int thingId);

    /// <summary>
    /// Called when the count in a specific <see cref="Transferable"/> was changed.
    /// </summary>
    /// <param name="tr">Transferable whose count was changed.</param>
    void Notify_CountChanged(Transferable tr);
}