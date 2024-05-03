// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using RimWorld.Planet;

namespace AdaptiveStorage;

public class ContentsITab : ITab_ContentsBase
{
	public static readonly string
		LabelKey = Strings.Keys.TabTransporterContents,
		LabelTranslated = LabelKey.Translate();
	
	public BetterQuickSearchWidget QuickSearchWidget { get; } = new() { MaxSearchTextLength = int.MaxValue };

	protected new object SelObject
		=> base.SelObject is var obj && obj is Thing thing ? thing.GetInnerIfMinified() : obj;
	
	public override IList<Thing> container => SelObject.StoredThings();

	// this is a confirmation box, not just a message
	public override bool UseDiscardMessage => false;

	public override bool IsVisible
		=> SelObject is ISlotGroupParent or (IThingHolder and IHaulDestination);

	public new virtual float ThingRowHeight => 30f;

	public const float
		SEARCH_WIDGET_MARGIN = 3f,
		BORDER_MARGIN = SpaceBetweenItemsLists,
		TOP_PADDING = 8f;

	protected GUIScope.ScrollViewStatus _scrollViewStatus = new();

	public ContentsITab()
	{
		labelKey = LabelKey;
		containedItemsKey = Strings.Keys.ContainedItems;
	}

	protected override void FillTab()
	{
		if (Event.current.type == EventType.Layout) // this gets sent every frame but can only draw behind every window
			return;

		canRemoveThings = SelObject is Thing { Faction: { } faction } && faction == Faction.OfPlayer;
		
		thingsToSelect.Clear();
		var outRect = new Rect(new(), size).ContractedBy(BORDER_MARGIN);
		outRect.yMin += TOP_PADDING;
		
		using var fontScope = new GUIScope.Font(GameFont.Small);
		
		var curY = 0f;
		DoItemsLists(outRect, ref curY);
		
		TrySelectAndJump();
	}

	protected override void DoItemsLists(Rect outRect, ref float curY)
	{
		var storedThings = container;
		
		if (storedThings is Thing[] thingArray)
		{
			Array.Sort(thingArray, (Comparison<Thing>)(static (a, b) =>
			{
				var result = string.Compare(a.GetInnerIfMinified().LabelNoCount, b.GetInnerIfMinified().LabelNoCount,
					StringComparison.OrdinalIgnoreCase);
				if (result == 0)
					result = a.stackCount.CompareTo(b.stackCount);
				
				return result;
			}));
		}

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

			if (!filter.Matches(thing.GetInnerIfMinified().def))
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

	private void TryDrawSlider(ref Rect outRect, ref float curY)
	{
		if (SelObject is not ThingClass adaptive)
			return;

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
				Strings.Stacks(storedThings.Count, SelObject.CurrentSlotLimit())}, {
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

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L216-L235
	protected override void OnDropThing(Thing t, int count)
	{
		if (SelObject is not Thing thing)
			return;
		
		var dropCell = thing.Position + DropOffset;
		var map = thing.Map;
		t = t.SplitOff(count);

		if (!GenDrop.TryDropSpawn(t, dropCell, map, ThingPlaceMode.Near, out var resultingThing, null,
			cell => !map.ContainsStorageBuildingAt(cell)))
		{
			GenSpawn.Spawn(resultingThing = t, dropCell, map);
		}

		if (resultingThing.TryGetComp<CompForbiddable>() is { } compForbiddable)
			compForbiddable.Forbidden = true;

		if (resultingThing is null || !resultingThing.Spawned || resultingThing.Position == dropCell)
			Messages.Message(Strings.Translated.ASF_MapFilled, new(dropCell, map), MessageTypeDefOf.NegativeEvent, false);
	}

	protected void DoThingRow(int index, Thing thing, float mass, float width, ref float curY,
		Action<int> discardAction)
	{
		var count = thing.stackCount;
		var thingDef = thing.def;
		
		var rect = new Rect(0f, curY, width, ThingRowHeight);
		
		if ((index & 1) == 0)
			Widgets.DrawAltRect(rect);
		
		if (canRemoveThings)
		{
			DrawRemoveSpecificCountButton(thingDef, count, discardAction, ref rect);
			DrawRemoveAllButton(count, thing, discardAction, ref rect);
		}

		DrawInfoCardButton(thing, ref rect);
		DrawForbidToggle(thing, ref rect);

		if (Mouse.IsOver(rect))
			DrawHighlightTexture(rect);

		if (thingDef.DrawMatSingle != null && thingDef.DrawMatSingle.mainTexture != null)
			Widgets.ThingIcon(new(4f, curY, ThingIconSize, ThingIconSize), thing);

		DrawMassLabel(mass, ref rect);
		DrawRotLabel(thing, ref rect);
		DrawLabel(thing, rect);

		TooltipHandler.TipRegion(rect, string.Concat(thing.LabelCap, "\n", thing.DescriptionDetailed));
		
		if (Widgets.ButtonInvisible(rect))
			SelectLater(thing);

		if (Mouse.IsOver(rect))
			TargetHighlighter.Highlight(thing); // arrow towards the thing

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
		var labelRect = rect with { x = ThingLeftX, width = rect.width - ThingLeftX };

		using (new GUIScope.TextAnchor(TextAnchor.MiddleLeft))
		using (new GUIScope.Color(ThingLabelColor))
		using (new GUIScope.WordWrap(false))
			Widgets.Label(labelRect, thing.LabelCap.StripTags().Truncate(labelRect.width));
	}

	public void SelectLater(Thing thing)
	{
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
		
		if (thing is ThingWithComps thingWithComps && thingWithComps.GetComp<CompForbiddable>() is { } compForbiddable)
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