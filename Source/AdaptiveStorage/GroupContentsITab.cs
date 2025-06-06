// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage;

public class GroupContentsITab : ContentsITab
{
	public virtual StorageGroup? Group => (SelObject as IStorageGroupMember)?.Group;
	
	public override bool IsVisible => base.IsVisible && Group is { MemberCount: > 1 };

	protected override bool CanRemoveThings
		=> Group?.members.Exists(static member
				=> member is Thing { Faction: { } faction } && faction == Faction.OfPlayer)
			?? false;

	protected override int SlotLimit => Group?.members.Sum(static member => member.CurrentSlotLimit()) ?? 0;

	protected override bool DisplaySlider => false;

	public override IList<Thing> container
	{
		get
		{
			var group = Group;
			if (group is not { MemberCount: > 1 })
				return Array.Empty<Thing>();
			
			using var pooledList = new PooledList<Thing>();
			var list = pooledList.List;

			var groupMembers = group.members;
			for (var i = 0; i < groupMembers.Count; i++)
				list.AddRange(groupMembers[i].StoredThings());
			
			return list.ToArray();
		}
	}

	public static GroupContentsITab Instance
		=> _instance ??= (GroupContentsITab)InspectTabManager.GetSharedInstance(typeof(GroupContentsITab));

	protected override bool CanRemoveThing(Thing thing)
		=> base.CanRemoveThing(thing)
			&& thing.StoringThing() is { Faction: { } faction }
			&& faction == Faction.OfPlayer;

	public GroupContentsITab() => labelKey = Strings.Keys.StorageGroup;
	
	private static GroupContentsITab? _instance;
}
