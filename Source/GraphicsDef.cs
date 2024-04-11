// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Xml;
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GraphicsDef : Def
{
	public List<StorageGraphic> graphics = [];

	public Columns<ItemGraphic>? itemGraphics;
	
	public ThingFilter
		allowedFilter = new(),
		forbiddenFilter = new();

	public AllowedRequirement allowedRequirement = AllowedRequirement.Any;

	public bool
		showContainedItems = true,
		useDominantContentColor,
		onlyAllowRequiredThingDefs;

	public StackBehaviour stackBehaviour;

	public int randomSelectionWeight = 1,
		minimumThingCount = int.MinValue,
		minimumAllowedThingCount = int.MinValue,
		maximumThingCount = int.MaxValue;

	public List<ThingDef>?
		requiredThingDefs,
		disallowedThingDefs;

	public List<ThingCategoryDef>? allowedThingCategories;
	
	public ThingDef? targetDef;

	public List<ThingDef> targetDefs = [];

	[Unsaved]
	private bool
		_hasEmptyStackGraphic,
		_forbidsNothing;

	public bool HasEmptyStackGraphic => _hasEmptyStackGraphic;

	public bool ForbidsNothing => _forbidsNothing;
	
	public static Dictionary<ThingDef, List<GraphicsDef>> Database { get; } = new();

	public static Func<GraphicsDef, uint> PositiveWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight is var weight and > 0 ? (uint)weight : 0u;

	public static Func<GraphicsDef, uint> NegativeWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight is var weight and < 0 ? (uint)-weight : 0u;

	public static Func<GraphicsDef, uint> NullWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight == 0 ? 1u : 0u;

	private static Type? _deepStorageITabType
		= Type.GetType("LWM.DeepStorage.ITab_DeepStorage_Inventory, LWM.DeepStorage");

	public override void ResolveReferences()
	{
		if (onlyAllowRequiredThingDefs)
			allowedRequirement = AllowedRequirement.All;
		
		if (randomSelectionWeight < 0)
			randomSelectionWeight = 0;
		
		targetDefs ??= [];
		
		if (targetDef != null)
			targetDefs.AddDistinct(targetDef);

		graphics ??= [];
		ResolveForbiddenFilter();
		ResolveAllowedFilter();
	}

	private void ResolveForbiddenFilter()
	{
		forbiddenFilter ??= new();
		forbiddenFilter.ResolveReferences();
		
		if (disallowedThingDefs is [_,..])
			forbiddenFilter.SetAllowAll(disallowedThingDefs, true);

		_forbidsNothing = forbiddenFilter.AllowedDefCount == 0;
	}

	private void ResolveAllowedFilter()
	{
		allowedFilter ??= new();

		if (allowedThingCategories is [_,..])
			(allowedFilter.categories ??= []).AddRangeDistinct(allowedThingCategories.Select(static cat => cat.defName));

		allowedFilter.ResolveReferences();

		if (requiredThingDefs is [_,..])
			allowedFilter.SetAllowAll(requiredThingDefs, true);
		
		if (allowedFilter.AllowedDefCount == 0)
			allowedFilter.SetAllowAll(StorageSettings.EverStorableFixedSettings().filter);

		allowedFilter.SetAllowAll(forbiddenFilter.AllowedThingDefs, false);
	}

	public void Initialize()
	{
		foreach (var def in targetDefs)
			InitializeOnDef(def);

		if (graphics.Count == 0)
			graphics.Add(new() { graphicData = targetDefs.FirstOrDefault()?.graphicData });
		
		foreach (var graphic in graphics)
			graphic.Initialize();

		graphics.Sort(static (x, y)
			=> x.minimumStackCount.CompareTo(y.minimumStackCount));

		if (targetDefs.Count > 0)
			itemGraphics?.Initialize(targetDefs);

		var minimumStackCountOnGraphics = graphics.Min(static g => g.minimumStackCount);
		_hasEmptyStackGraphic = minimumStackCountOnGraphics < 1;
		
		if (minimumAllowedThingCount < 0 && !HasEmptyStackGraphic)
			minimumAllowedThingCount = 1;

		if (minimumThingCount < minimumAllowedThingCount)
			minimumThingCount = minimumAllowedThingCount;

		if (minimumThingCount < minimumStackCountOnGraphics)
			minimumThingCount = minimumStackCountOnGraphics;
	}

	public bool Allows(int thingCount) => thingCount >= minimumThingCount && thingCount <= maximumThingCount;

	public bool Allows(ThingDef thingDef) => allowedFilter.Allows(thingDef);

	public bool Allows(Thing thing) => allowedFilter.Allows(thing);

	public bool Allows<T>(T things) where T : IList<Thing>
	{
		if (!Allows(things.Count))
			return false;

		var allowedThingCount = 0;
		var remainingThingCount = 0;
		for (var i = things.Count; i-- > 0;)
		{
			var thing = things[i];
			if (Allows(thing))
				allowedThingCount++;
			else if (OnlyAllowRequiredThingDefs || Forbids(thing))
				return false;
			else
				remainingThingCount++;
		}

		return allowedThingCount >= minimumAllowedThingCount
			&& allowedRequirement switch
			{
				AllowedRequirement.Majority => allowedThingCount > remainingThingCount,
				AllowedRequirement.Minority => allowedThingCount < remainingThingCount,
				_ => true
			};
	}

	public bool Allows(IList<ThingDef> thingDefs)
	{
		if (thingDefs.Count == 0)
			return HasEmptyStackGraphic;

		var hasAnyAllowedThing = false;
		for (var i = thingDefs.Count; i-- > 0;)
		{
			var thingDef = thingDefs[i];
			if (Allows(thingDef))
				hasAnyAllowedThing = true;
			else if (OnlyAllowRequiredThingDefs || Forbids(thingDef))
				return false;
		}

		return hasAnyAllowedThing;
	}

	private bool OnlyAllowRequiredThingDefs => allowedRequirement == AllowedRequirement.All;

	public bool Forbids(ThingDef thingDef) => !ForbidsNothing && forbiddenFilter.Allows(thingDef);

	public bool Forbids(Thing thing) => !ForbidsNothing && forbiddenFilter.Allows(thing);

	private void InitializeOnDef(ThingDef def)
	{
		if (!typeof(ThingClass).IsAssignableFrom(def.thingClass))
			def.thingClass = typeof(ThingClass);

		var contentsITabType = typeof(ContentsITab);
		if (!def.inspectorTabs.Contains(contentsITabType) && !def.inspectorTabs.Contains(_deepStorageITabType))
		{
			def.inspectorTabs.Add(contentsITabType);
			(def.inspectorTabsResolved ??= []).Add(InspectTabManager.GetSharedInstance(contentsITabType));
		}

		if (!Database.TryGetValue(def, out var list))
			Database[def] = list = [];

		if (!list.Contains(this))
			list.Add(this);
	}

	public override IEnumerable<string> ConfigErrors()
	{
		if (randomSelectionWeight < 0)
			yield return ErrorForDef("has negative random selection weight");
		
		foreach (var configError in base.ConfigErrors())
			yield return configError;
		
		if (targetDef is null && targetDefs is not [_,..])
		{
			yield return ErrorForDef("contains no targetDef or targetDefs node. It will not be used anywhere");
			yield break;
		}

		var tempDefsToErrorCheck = new List<ThingDef>();
		
		if (targetDefs is [_,..])
			tempDefsToErrorCheck.AddRange(targetDefs);
		
		if (targetDef != null && !tempDefsToErrorCheck.Contains(targetDef))
			tempDefsToErrorCheck.Add(targetDef);

		foreach (var defToCheck in tempDefsToErrorCheck)
		{
			if (!typeof(Building_Storage).IsAssignableFrom(defToCheck.thingClass))
			{
				yield return ErrorForDef($"contains the target def {defToCheck} with a thing class that is not "
					+ "assignable to Building_Storage. It will get replaced with AdaptiveStorage.ThingClass and may "
					+ "not work correctly");
			}
			else if (!typeof(ThingClass).IsAssignableFrom(defToCheck.thingClass))
			{
				if (defToCheck.thingClass != typeof(Building_Storage))
				{
					yield return ErrorForDef($"contains the target def {defToCheck} with a custom "
						+ "Building_Storage subclass that is not assignable to AdaptiveStorage.ThingClass. It will "
						+ "get replaced and may not work correctly.");
				}
			}
		}

		if (CheckForNullGraphics() is { } nullGraphicsError)
		{
			yield return nullGraphicsError;
			yield break;
		}

		if (graphics != null!)
		{
			foreach (var error in CheckForNullGraphicsInCollection(graphics, "graphics list"))
				yield return error;
		}
	}

	private IEnumerable<string> CheckForNullGraphicsInCollection(List<StorageGraphic> collection, string name)
	{
		for (var i = collection.Count; i-- > 0;)
		{
			if (collection[i]?.graphicData != null!)
				continue;

			yield return ErrorForDef($"loaded with null graphic at index {i} in {name}");

			collection.RemoveAt(i);
		}
	}

	private string? CheckForNullGraphics()
		=> graphics != null! ? null : ErrorForDef("loaded with null graphics");

	private string ErrorForDef(string message)
		=> $"AdaptiveStorage.GraphicsDef {defName} from mod {modContentPack?.Name ?? "NULL"} {message}";
	
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Columns<T> where T : IHasPosition, new()
	{
		public List<Rows<T>> columns = [];

		public T? defaults;

		public void Initialize(List<ThingDef> targetDefs)
		{
			var targetColumnCount = targetDefs.Max(static def => def.size.x);
			var targetRowCount = targetDefs.Max(static def => def.size.z);
		
			var lastColumnPosition = -1;
		
			for (var i = 0; i < targetColumnCount; i++)
			{
				var rows = FillAndShuffle(columns, ref i, ref lastColumnPosition).rows;
			
				var lastRowPosition = -1;

				for (var j = 0; j < targetRowCount; j++)
					FillAndShuffle(rows, ref j, ref lastRowPosition);
			}
		}

		private static TItem FillAndShuffle<TItem>(List<TItem> list, ref int i, ref int lastPosition)
			where TItem : IHasPosition, new()
		{
			while (list.Count <= i)
				list.Add(new());

			var item = list[i];
			item.Position ??= lastPosition + 1;
			var itemPosition = item.Position.Value;

			if (itemPosition < i)
			{
				list.RemoveAt(i);
				list.Insert(itemPosition, item);

				if (itemPosition == list[itemPosition + 1].Position)
					list.RemoveAt(itemPosition + 1);

				i = itemPosition;
			}
			else
			{
				while (itemPosition > i)
					list.Insert(i++, new());
			}

			lastPosition = itemPosition;
			return item;
		}
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Rows<T> : IHasPosition where T : IHasPosition
	{
		public int? position;
		public List<T> rows = [];
	
		int? IHasPosition.Position
		{
			get => position;
			set => position = value;
		}
	
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			if (xmlRoot.Name.Length > 1 && int.TryParse(xmlRoot.Name[1..], out var value))
				position = value;

			foreach (XmlNode childNode in xmlRoot.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Comment || childNode.Name != nameof(rows))
					continue;
			
				rows = DirectXmlToObject.ObjectFromXml<List<T>>(childNode, false);
			}
		}
	}

	public enum AllowedRequirement
	{
		Any,
		All,
		Majority,
		Minority
	}
}