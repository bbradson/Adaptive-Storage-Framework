using Verse;

namespace Multiplayer.API;

/// <summary>
/// <para>Sessions inheriting from this class contain persistent data.</para>
/// <para>When inheriting from this class, remember to call <c>base.ExposeData()</c> to let it handle <see cref="Session.SessionId"/></para>
/// <para>Persistent data:</para>
/// <list type="bullet">
///     <item>Serialized into XML using RimWorld's Scribe system</item>
///     <item>Save-bound: survives a server restart</item>
/// </list>
/// </summary>
public abstract class ExposableSession : Session, IExposable
{
    /// <inheritdoc cref="Session(Map)"/>
    protected ExposableSession(Map map) : base(map) { }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref sessionId, "sessionId");
    }
}