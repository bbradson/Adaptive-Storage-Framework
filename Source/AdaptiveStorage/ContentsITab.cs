// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery.Pools;
using Multiplayer.API;
using RimWorld.Planet;

namespace AdaptiveStorage;

[PublicAPI]
public class ContentsITab : ITab_ContentsBase
{
	public static readonly string
		LabelKey = Strings.Keys.TabTransporterContents,
		LabelTranslated = LabelKey.Translate();
	
	public BetterQuickSearchWidget QuickSearchWidget { get; } = new() { MaxSearchTextLength = int.MaxValue };

	protected new object SelObject
		=> base.SelObject is var obj && obj is Thing thing ? thing.GetInnerIfMinified() : obj;

	public List<object> SelectedStorages
	{
		get
		{
			var currentSelObjects = AllSelObjects;
			if (currentSelObjects._version != _selObjectsVersion)
			{
				UpdateSelectedStorages(field);
				_selObjectsVersion = currentSelObjects._version;
			}

			return field;
		}
	} = [];

	public override IList<Thing> container
	{
		get
		{
			var storages = SelectedStorages;
			if (storages.Count == 1)
				return storages[0].StoredThings();
			
			using var pooledList = new PooledList<Thing>();
			var list = pooledList.List;

			for (var i = 0; i < storages.Count; i++)
				list.AddRange(storages[i].StoredThings());
			
			return list.ToArray();
		}
	}

	protected virtual int SlotLimit => SelectedStorages.Sum(static storage => storage.CurrentSlotLimit());

	protected virtual bool CanRemoveThings
		=> SelectedStorages.TrueForAll(static storage
			=> storage is Thing { Faction: { } faction } && faction == Faction.OfPlayer);

	// this is a confirmation box, not just a message
	public override bool UseDiscardMessage => false;

	public override bool IsVisible => SelectedStorages.Count > 0;

	protected virtual bool DisplaySlider => SelectedStorages is [ThingClass];

	public new virtual float ThingRowHeight => 30f;

	public const float
		SEARCH_WIDGET_MARGIN = 3f,
		BORDER_MARGIN = SpaceBetweenItemsLists,
		TOP_PADDING = 8f,
		LABEL_MIN_GAP = 4f;

	protected readonly GUIScope.ScrollViewStatus _scrollViewStatus = new();
	// protected readonly Dictionary<string, TaggedString> _truncatedLabelCache = [];
	// protected float _truncatedLabelCacheWidth;

	private int _selObjectsVersion = -3;

	public ContentsITab()
	{
		labelKey = LabelKey;
		containedItemsKey = Strings.Keys.ContainedItems;
	}

	// protected Dictionary<string, TaggedString> GetTruncatedLabelCacheForWidth(float width)
	// {
	// 	if (Math.Abs(width - _truncatedLabelCacheWidth) > 0.01f)
	// 	{
	// 		_truncatedLabelCache.Clear();
	// 		_truncatedLabelCacheWidth = width;
	// 	}
	//
	// 	return _truncatedLabelCache;
	// }

	// protected override void CloseTab()
	// {
	// 	base.CloseTab();
	// 	_truncatedLabelCache.Clear();
	// }
	//
	// public override void OnOpen()
	// {
	// 	base.OnOpen();
	// 	_truncatedLabelCache.Clear();
	// }

	protected virtual void UpdateSelectedStorages(List<object> selectedStorages)
	{
		selectedStorages.Clear();
		
		var currentSelObjects = AllSelObjects;
		for (var i = currentSelObjects.Count; --i >= 0;)
		{
			var selObject = currentSelObjects[i];
			if (IsVisibleFor(selObject is Thing thing ? selObject = thing.GetInnerIfMinified() : selObject))
				selectedStorages.Add(selObject);
		}
	}

	public virtual bool IsVisibleFor(object obj) => obj is (ISlotGroupParent or (IThingHolder and IHaulDestination));

	protected override void FillTab()
	{
		if (Event.current.type == EventType.Layout) // this gets sent every frame but can only draw behind every window
			return;

		canRemoveThings = CanRemoveThings;
		
		thingsToSelect.Clear();
		var outRect = new Rect(new(), size).ContractedBy(BORDER_MARGIN);
		outRect.yMin += TOP_PADDING;
		
		using var fontScope = new GUIScope.Font(GameFont.Small);
		
		var curY = 0f;
		DoItemsLists(outRect, ref curY);
		
		TrySelectAndJump();
	}

	protected virtual bool CanRemoveThing(Thing thing) => canRemoveThings;

	protected override void DoItemsLists(Rect outRect, ref float curY)
	{
		var storedThings = container;
		
		if (storedThings is Thing[] thingArray)
			SortThings(thingArray);

		using var massList = new ScopedStatList(storedThings, StatDefOf.Mass);
		using var groupScope = new GUIScope.WidgetGroup(outRect);
		outRect.position = Vector2.zero;
		
		DrawHeader(ref outRect, ref curY, storedThings, massList);
		
		TryDrawSlider(ref outRect, ref curY);
		
		DrawSearchWidget(ref outRect);

		using var scrollView = new GUIScope.ScrollView(outRect, _scrollViewStatus);
		curY = ref scrollView.Height;
		var inRectWidth = scrollView.Rect.width;
		
		var hasAnyStoredThing = false;
		var filterHasAnyMatches = false;
		var filter = QuickSearchWidget.Filter;
		var thingRowHeight = ThingRowHeight;

		for (var i = 0; i < storedThings.Count; i++)
		{
			if (storedThings[i] is not { } thing)
				continue;

			hasAnyStoredThing = true;

			if (!filter.Matches((thing.GetInnerIfMinified() ?? thing).def))
				continue;

			if (scrollView.CanCull(thingRowHeight, curY))
			{
				curY += thingRowHeight;
				continue;
			}

			filterHasAnyMatches = true;
			
			DoThingRow(i, thing, massList[i], inRectWidth, ref curY, count => OnDropThing(thing, count));
		}
		
		if (!hasAnyStoredThing)
			Widgets.NoneLabel(ref curY, inRectWidth);

		QuickSearchWidget.NoResultsMatched = !filterHasAnyMatches;
	}

	private static void SortThings(Thing[] thingArray) => Array.Sort(thingArray, CompareLabelsThenStackCount);

	private static int CompareLabelsThenStackCount(Thing? a, Thing? b)
	{
		if (a is null)
			return b is null ? 0 : 1;

		if (b is null)
			return -1;

		var result = string.Compare(a.GetInnerIfMinified()?.LabelNoCount, b.GetInnerIfMinified()?.LabelNoCount,
			StringComparison.OrdinalIgnoreCase);

		if (result == 0)
			result = a.stackCount.CompareTo(b.stackCount);

		return result;
	}

	private void TryDrawSlider(ref Rect outRect, ref float curY)
	{
		if (!DisplaySlider)
			return;

		var adaptive = (ThingClass)SelectedStorages[0];
		var sliderRect = outRect with { height = Text.LineHeight / 2f };
		var totalSlots = adaptive.TotalSlots;
		var currentSlotLimit = Math.Min(adaptive.CurrentSlotLimit, totalSlots);
		var newSlotLimit = Mathf.RoundToInt(
#if V1_4
			Widgets.HorizontalSlider_NewTemp(
#else
			Widgets.HorizontalSlider(
#endif
				sliderRect, currentSlotLimit, 0f, totalSlots));
		
		if (newSlotLimit != currentSlotLimit)
			adaptive.CurrentSlotLimit = newSlotLimit;
		
		curY += sliderRect.height;
		outRect.yMin += sliderRect.height;
	}

	private void DrawHeader(ref Rect outRect, ref float curY, IList<Thing> storedThings, ScopedStatList massList)
	{
		using (new GUIScope.FontSize(15))
		{
			Widgets.ListSeparator(ref curY, outRect.width, $"{Strings.Translated.ContainedItems} ({
				Strings.Stacks(storedThings.Count, SlotLimit)}, {
					massList.Sum.ToStringMass()})");
			outRect.yMin += curY;
		}
	}

	private void DrawSearchWidget(ref Rect outRect)
	{
		QuickSearchWidget.OnGUI(outRect with
		{
			y = outRect.y + SEARCH_WIDGET_MARGIN, height = ThingRowHeight - (SEARCH_WIDGET_MARGIN * 2f)
		});
		outRect.yMin += ThingRowHeight;
	}

	protected override void OnDropThing(Thing t, int count) => EjectThing(t, count, DropOffset);

	[SyncMethod(SyncContext.MapSelected, cancelIfAnyArgNull = true, cancelIfNoSelectedMapObjects = true)]
	protected static void EjectThing(Thing item, int count, IntVec3 dropOffset = default)
	{
		if (item.stackCount < count) // for simultaneous button clicks with multiplayer
			count = item.stackCount;

		if (count < 1)
			return;
		
		switch (item.StoringThing())
		{
			case ThingClass adaptive:
				adaptive.Eject(item, count, dropOffset: dropOffset);
				break;
			case { } thing:
				ThingMakerUtility.EjectFromStorage(thing, item, count, dropOffset: dropOffset);
				break;
		}
	}

	protected void DoThingRow(int index, Thing thing, float mass, float width, ref float curY,
		Action<int> discardAction)
	{
		try
		{
			var count = thing.stackCount;
			var thingDef = thing.def;
		
			var rect = new Rect(0f, curY, width, ThingRowHeight);
		
			if ((index & 1) == 0)
				Widgets.DrawAltRect(rect);
		
			if (CanRemoveThing(thing))
			{
				DrawRemoveSpecificCountButton(thingDef, count, discardAction, ref rect);
				DrawRemoveAllButton(count, thing, discardAction, ref rect);
			}

			DrawInfoCardButton(thing, ref rect);
			DrawForbidToggle(thing, ref rect);

			if (Mouse.IsOver(rect))
				DrawHighlightTexture(rect);

			if (thingDef.DrawMatSingle is var mat && mat && mat.mainTexture)
				Widgets.ThingIcon(new(4f, curY, ThingIconSize, ThingIconSize), thing);

			DrawMassLabel(mass, ref rect);
			DrawRotLabel(thing, ref rect);
			DrawLabel(thing, rect);

			TooltipHandler.TipRegion(rect, string.Concat(thing.LabelCap, "\n", thing.DescriptionDetailed));
		
			if (Widgets.ButtonInvisible(rect))
				SelectLater(thing);

			if (Mouse.IsOver(rect))
				TargetHighlighter.Highlight(thing); // arrow towards the thing
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}

		curY += ThingRowHeight;
	}

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L159-L173
	private static void DrawRotLabel(Thing thing, ref Rect rect)
	{
		var compRottable = thing.TryGetComp<CompRottable>();
		if (compRottable is null)
			return;

		var rotTicks = compRottable.TicksUntilRotAtCurrentTemp;
		if (rotTicks >= GenDate.TicksPerYear * 10)
			return;

		rect.width -= CaravanThingsTabUtility.MassColumnWidth;

		var labelRect = rect with { x = rect.width, width = CaravanThingsTabUtility.MassColumnWidth };

		using (new TextBlock(null, TextAnchor.MiddleLeft, false))
		using (new GUIScope.Color(Color.yellow))
			Widgets.Label(labelRect, (rotTicks / (float)GenDate.TicksPerDay).ToString("0.#"));

		TooltipHandler.TipRegion(labelRect, Strings.Translated.DaysUntilRotTip);
	}
	
	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L155-L158
	private static void DrawMassLabel(float mass, ref Rect rect)
	{
		rect.width -= CaravanThingsTabUtility.MassColumnWidth;
		var labelRect = rect with { x = rect.width, width = CaravanThingsTabUtility.MassColumnWidth };
		
		CaravanThingsTabUtility.DrawMass(mass, labelRect);
		TooltipHandler.TipRegion(labelRect, Strings.MassDescription);
	}

	private static void DrawHighlightTexture(in Rect rect)
	{
		using (new GUIScope.Color(ThingHighlightColor))
			GUI.DrawTexture(rect, TexUI.HighlightTex);
	}

	private static void DrawLabel(Thing thing, in Rect rect)
	{
		var labelRect = rect with { x = ThingLeftX, width = rect.width - ThingLeftX - LABEL_MIN_GAP };

		using (new GUIScope.TextAnchor(TextAnchor.MiddleLeft))
		using (new GUIScope.Color(GetThingLabelColor(thing)))
		using (new GUIScope.WordWrap(false))
		{
			// looks better without truncating. Colors applied to partly affected text get lost with Truncate
			// var thingLabel = thing.Label;
			// var cache = GetTruncatedLabelCacheForWidth(labelRect.width);
			// if (!cache.TryGetValue(thingLabel, out var truncatedLabel))
			// 	truncatedLabel = ((TaggedString)thing.LabelCap).Truncate(labelRect.width - LABEL_MIN_GAP);
			
			Widgets.Label(labelRect, thing.LabelCap);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[PublicAPI]
	private static Color GetThingLabelColor(Thing thing) => ThingLabelColor;

	public void SelectLater(Thing thing)
	{
		if (thing.StoringAdaptiveStorage() is { AllowItemForbiddingAccess: false })
			return;
		
		thingsToSelect.Clear();
		thingsToSelect.Add(thing);
	}

	public void TrySelectAndJump()
	{
		if (!thingsToSelect.Any())
			return;
		
		ITab_Pawn_FormingCaravan.SelectNow(thingsToSelect);
		thingsToSelect.Clear();
	}

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L132-L145
	private static void DrawForbidToggle(Thing thing, ref Rect rect)
	{
		var x = rect.width - Widgets.CheckboxSize;
		var y = rect.y + ((rect.height - Widgets.CheckboxSize) / 2f);
		
		if (thing is ThingWithComps thingWithComps
			&& thingWithComps.GetComp<CompForbiddable>() is { } compForbiddable
			&& thing.StoringAdaptiveStorage() is not { AllowItemForbiddingAccess: false })
		{
			var checkOn = !compForbiddable.Forbidden;
			var previousCheckOnState = checkOn;

			TooltipHandler.TipRegion(new(x, y, Widgets.CheckboxSize, Widgets.CheckboxSize),
				checkOn
					? Strings.TranslatedWithBackup.CommandNotForbiddenDesc
					: Strings.TranslatedWithBackup.CommandForbiddenDesc);
			
			Widgets.Checkbox(x, y, ref checkOn, paintable: true);

			if (checkOn != previousCheckOnState)
				compForbiddable.Forbidden = !checkOn;
		}
		else
		{
			Widgets.CheckboxDraw(x, y, true, true);
		}

		rect.width -= Widgets.CheckboxSize;
	}

	private void DrawRemoveAllButton(int count, Thing thing, Action<int> discardAction, ref Rect rect)
	{
		var buttonRect = NextButtonRect(rect);
		TooltipHandler.TipRegion(buttonRect, Strings.Translated.DropThing);
		
		if (Widgets.ButtonImage(buttonRect, CaravanThingsTabUtility.AbandonButtonTex))
		{
			if (UseDiscardMessage)
			{
				var text = thing is Pawn pawn ? pawn.LabelShortCap : thing.def.label;

				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
					Strings.Translated.ConfirmRemoveItemDialog.Formatted(text), () => discardAction(count)));
			}
			else
			{
				discardAction(count);
			}
		}

		rect.width -= Widgets.CheckboxSize;
	}

	private static void DrawRemoveSpecificCountButton(Def thingDef, int count, Action<int> discardAction, ref Rect rect)
	{
		if (count != 1)
		{
			var buttonRect = NextButtonRect(rect);
			TooltipHandler.TipRegion(buttonRect, Strings.Translated.ASF_DropSpecificCount);
			
			if (Widgets.ButtonImage(buttonRect, CaravanThingsTabUtility.AbandonSpecificCountButtonTex))
			{
				Find.WindowStack.Add(new Dialog_Slider(Strings.Translated.RemoveSliderText.Formatted(thingDef.label), 1,
					count, discardAction));
			}
		}

		rect.width -= Widgets.CheckboxSize;
	}

	private static Rect NextButtonRect(in Rect rect)
		=> new(rect.width - Widgets.CheckboxSize, rect.y + ((rect.height - Widgets.CheckboxSize) / 2f),
			Widgets.CheckboxSize, Widgets.CheckboxSize);

	private static void DrawInfoCardButton(Thing thing, ref Rect rect)
	{
		rect.width -= Widgets.CheckboxSize;
		Widgets.InfoCardButton(rect.width, rect.y, thing);
	}
}
