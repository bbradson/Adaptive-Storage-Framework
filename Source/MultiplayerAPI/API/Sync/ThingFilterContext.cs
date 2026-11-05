using System.Collections.Generic;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// A ThingFilter context provides information for syncing ThingFilter interactions.
/// Inheriting objects should store the ThingFilter's owner in a record property.
/// The type exists because vanilla ThingFilters don't store references to their owners.
/// </summary>
public abstract record ThingFilterContext : ISyncSimple
{
    public abstract ThingFilter Filter { get; }
    public abstract ThingFilter ParentFilter { get; }
    public virtual IEnumerable<SpecialThingFilterDef> HiddenFilters => null;
}