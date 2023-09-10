// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Xml;

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GraphicsDef : Def
{
	public static Dictionary<ThingDef, List<GraphicsDef>> Database { get; } = new();

	private static Type? _deepStorageITabType
		= Type.GetType("LWM.DeepStorage.ITab_DeepStorage_Inventory, LWM.DeepStorage");
	
	public ThingDef? targetDef;

	public List<ThingDef>? targetDefs;
	
	public List<StorageGraphic> graphics = new();

	public Columns<ItemGraphic>? itemGraphics;

	public bool
		showContainedItems = true,
		useDominantContentColor,
		onlyAllowRequiredThingDefs;

	public StackBehaviour stackBehaviour;

	public int randomSelectionWeight = 1;

	public List<ThingDef>?
		requiredThingDefs,
		disallowedThingDefs;

	public List<ThingCategoryDef>? allowedThingCategories;

	public void Initialize()
	{
		targetDefs ??= new();
		
		if (targetDef != null)
			targetDefs.AddDistinct(targetDef);

		foreach (var def in targetDefs)
			InitializeOnDef(def);
		
		foreach (var graphic in graphics)
			graphic.Initialize();

		graphics.Sort(static (x, y)
			=> x.minimumStackCount.CompareTo(y.minimumStackCount));

		itemGraphics?.Initialize(targetDefs);
	}

	public bool Allows(ThingDef thingDef)
		=> (allowedThingCategories is not [_, ..] || allowedThingCategories.Overlaps(thingDef.thingCategories))
			&& (disallowedThingDefs is not [_, ..] || !disallowedThingDefs.Contains(thingDef))
			&& (requiredThingDefs is not [_, ..]
				|| !onlyAllowRequiredThingDefs
				|| requiredThingDefs.Contains(thingDef));

	public bool Allows(List<Thing> things)
	{
		if (things.Count == 0)
			return requiredThingDefs is not [_, ..] && graphics.Exists(static g => g.minimumStackCount < 1);

		for (var i = things.Count; i-- > 0;)
		{
			if (!Allows(things[i].def))
				return false;
		}

		return onlyAllowRequiredThingDefs
			|| requiredThingDefs is not [_,..]
			|| things.Overlaps(requiredThingDefs);
	}

	public bool Allows<T>(T thingDefs) where T : IList<ThingDef>
	{
		if (thingDefs.Count == 0)
			return requiredThingDefs is not [_, ..] && graphics.Exists(static g => g.minimumStackCount < 1);
		
		for (var i = thingDefs.Count; i-- > 0;)
		{
			if (!Allows(thingDefs[i]))
				return false;
		}

		return onlyAllowRequiredThingDefs
			|| requiredThingDefs is not [_,..]
			|| thingDefs.Overlaps(requiredThingDefs);
	}

	private void InitializeOnDef(ThingDef def)
	{
		if (!typeof(ThingClass).IsAssignableFrom(def.thingClass))
			def.thingClass = typeof(ThingClass);

		var contentsITabType = typeof(ContentsITab);
		if (!def.inspectorTabs.Contains(contentsITabType) && !def.inspectorTabs.Contains(_deepStorageITabType))
		{
			def.inspectorTabs.Add(contentsITabType);
			(def.inspectorTabsResolved ??= new()).Add(InspectTabManager.GetSharedInstance(contentsITabType));
		}

		if (!Database.TryGetValue(def, out var list))
			Database[def] = list = new();

		if (!list.Contains(this))
			list.Add(this);
	}

	public override IEnumerable<string> ConfigErrors()
	{
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
		public List<Rows<T>> columns = new();

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
		public List<T> rows = new();
	
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
}