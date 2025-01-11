// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[PublicAPI]
public class GraphicsDefSelector(GraphicsDef Def)
{
	public virtual bool AllowedFor(ThingClass building)
		=> (Def.allowedRotations is not [_, ..] rotations || rotations.Contains(building.Rotation))
			&& Allows(building.StoredThings.AsCellWise); // cellWise for count
	
	public virtual bool Allows(int thingCount)
		=> Def is var def
			&& thingCount >= def.minimumThingCount
			&& thingCount <= def.maximumThingCount;

	public virtual bool Allows(Thing thing) => Def.allowedFilter.Allows(thing);

	public virtual bool Allows<T>(T things) where T : IList<Thing>
	{
		var thingCount = things.Count;
		if (!Allows(thingCount))
			return false;

		var allowedThingCount = 0;
		var forbiddenThingCount = 0;
		var def = Def;
		var allowedRequirement = def.allowedRequirement;
		var forbiddenRequirement = def.forbiddenRequirement;
		var forbidsNothing = def.ForbidsNothing;
		
		for (var i = thingCount; --i >= 0;)
		{
			var thing = things[i];
			if (Allows(thing))
				allowedThingCount++;
			else if (allowedRequirement == AllowedRequirement.All)
				return false;
			
			if (forbidsNothing || !Forbids(thing))
				continue;

			if (forbiddenRequirement == AllowedRequirement.Any)
				return false;
			else
				forbiddenThingCount++;
		}

		return allowedThingCount >= def.minimumAllowedThingCount
			&& (SatisfiesFilterRequirement(allowedRequirement, allowedThingCount, thingCount - allowedThingCount)
				|| (thingCount <= 0 && allowedRequirement == AllowedRequirement.Any))
			&& (!SatisfiesFilterRequirement(forbiddenRequirement, forbiddenThingCount,
					thingCount - forbiddenThingCount)
				|| (thingCount <= 0 && forbiddenRequirement == AllowedRequirement.All));
	}

	protected virtual bool SatisfiesFilterRequirement(AllowedRequirement filterRequirement,
		int matchingThingCount, int remainingThingCount)
		=> filterRequirement switch
		{
			AllowedRequirement.Any => matchingThingCount > 0,
			AllowedRequirement.All => remainingThingCount <= 0,
			AllowedRequirement.Majority => matchingThingCount > remainingThingCount,
			AllowedRequirement.Minority => matchingThingCount < remainingThingCount,
			AllowedRequirement.MajorityOrEqual => matchingThingCount >= remainingThingCount,
			AllowedRequirement.MinorityOrEqual => matchingThingCount <= remainingThingCount,
			AllowedRequirement.Equal => matchingThingCount == remainingThingCount,
			AllowedRequirement.None => matchingThingCount <= 0,
			AllowedRequirement.AnyNot => remainingThingCount > 0,
			AllowedRequirement.Always => true,
			AllowedRequirement.Never => false,
			_ => true
		};

	public virtual bool Forbids(Thing thing) => Def.forbiddenFilter.Allows(thing);

	public virtual void ResolveReferences()
	{
	}
}