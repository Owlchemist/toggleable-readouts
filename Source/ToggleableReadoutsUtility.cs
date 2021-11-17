using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using static ToggleableReadouts.FastGUI;
using static ToggleableReadouts.ResourceBank;
 
namespace ToggleableReadouts
{
    internal static class ToggleableReadoutsUtility
	{
		static Dictionary<Def, ReadoutCache> readoutCache = new Dictionary<Def, ReadoutCache>();
		public static bool updateNow = true;
		public static Event eventCache;
		public static EventType eventType;
		static GUIContent guiContent = GUIContent.Temp("");
		static GUIStyle guiStyle;
		public static bool mouseOver;
		public static int ticker = 119;
		static Listing_ResourceReadout list;
		static int numOfCategories;
		static ThingCategoryDef[] rootThingCategories;

		public static void Setup()
		{
			DefDatabase<ThingCategoryDef>.AllDefsListForReading.ForEach(x => readoutCache.Add(x, new ReadoutCache(null, 0, x)));
			rootThingCategories = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Where(x => x.resourceReadoutRoot).ToArray();
			numOfCategories = rootThingCategories.Length;
			
			guiStyle = Text.fontStyles[1];
			list = new Listing_ResourceReadout(null);
			list.nestIndentWidth = 7f;
			list.lineHeight = 24f;
			list.verticalSpacing = 0f;
		}

		public static void DoReadoutCategorizedFast(ResourceReadout instance, Rect rect)
		{
			eventCache = Event.current;
			eventType = eventCache.type;

			if (updateNow = ++ticker == 120) ticker = 0;

			list.listingRect = rect;
			list.columnWidthInt = list.listingRect.m_Width;
			list.curX = list.curY = list.maxHeightColumnSeen = 0f;
			GUIClip.Internal_Push(rect, vector2zero, vector2zero, false);

			list.map = Current.gameInt.maps[(int)Current.gameInt.currentMapIndex];

			mouseOver = Mouse.IsOver(rect);
			for (int i = 0; i < numOfCategories; ++i)
			{
				DoCategoryFast(list, rootThingCategories[i].treeNode, 0, 32);
			}
			
			GUIClip.Internal_Pop();
			instance.lastDrawnHeight = Math.Max(list.CurHeight, list.maxHeightColumnSeen);
		}

		public static void DoCategoryFast(Listing_ResourceReadout list, TreeNode_ThingCategory node, int nestLevel, int openMask)
		{
			//Category expanded?
			bool expanded = (node.openBits & openMask) != 0;

			//Fetch or build the cache
			ReadoutCache readout = readoutCache[node.catDef];
			if (updateNow) readout.Update(list, nestLevel, node.catDef, expanded);			
			if (readout.value == 0) return;

			//Handle the collapsible button
			if (ButtonImageFast(readout.buttonRect, readout.controlID, eventCache, eventType, expanded ? TexButton.Collapse : TexButton.Reveal))
			{
				if (expanded) SoundDefOf.TabClose.PlayOneShotOnCamera(null);
				else SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
				node.SetOpen(openMask, !expanded);
				ticker = 119; //Update next tick (120)
			}
			
			//Background label container
			DrawTextureFast(readout.highlightRect, Listing_ResourceReadout.SolidCategoryBG, colorWhite);

			//Highlight and tooltip
			if (mouseOver && Mouse.IsOver(readout.containerRect))
			{
				DrawTextureFast(readout.containerRect, TexUI.HighlightTex, colorWhite);
				TooltipHandler.TipRegion(readout.containerRect, new TipSignal(node.catDef.LabelCap, node.catDef.GetHashCode()));
			}
			
			//Draw icon
			DrawTextureFast(readout.iconRect, node.catDef.icon, colorWhite);

			//Draw label
			guiContent.m_Text = readout.label;
			guiStyle.alignment = TextAnchor.MiddleLeft;
			guiStyle.Internal_Draw_Injected(ref readout.labelRect, guiContent, false, false, false, false);

			//Draw children
			list.EndLine();
			if (expanded && readout.things != null) DoCategoryChildrenFast(readout, list, nestLevel + 1, openMask);
		}

		static void DoCategoryChildrenFast(ReadoutCache readout, Listing_ResourceReadout list, int indentLevel, int openMask)
		{
			int length = readout.categories.Length;
			for (int i = 0; i < length; ++i)
			{
				DoCategoryFast(list, readout.categories[i], indentLevel, openMask);
			}
			length = readout.things.Count;
			for (int i = 0; i < length; i++)
			{
				ReadoutCache entry = readout.things[i];
				if (entry.value != 0 ) DoThingDefFast(entry, list, indentLevel + 1);
			}
		}

		static void DoThingDefFast(ReadoutCache readout, Listing_ResourceReadout list, int nestLevel)
		{
			ThingDef thingDef = readout.thingDef;

			//Compensate curY
			readout.containerRect.y = readout.highlightRect.y = readout.labelRect.y = list.curY;
			readout.iconRect.y = list.curY + readout.containerRect.height / 2f - readout.iconRect.height / 2f;
			
			if (mouseOver && Mouse.IsOver(readout.containerRect))
			{
				DrawTextureFast(readout.containerRect, TexUI.HighlightTex, colorWhite);
				TooltipHandler.TipRegion(readout.containerRect, new TipSignal(() => thingDef.LabelCap + ": " + thingDef.description.CapitalizeFirst(), (int)thingDef.shortHash));
			}
			DrawTextureFast(readout.iconRect, thingDef.uiIcon, thingDef.graphicData.color);

			guiContent.m_Text = readout.label;
			guiStyle.Internal_Draw_Injected(ref readout.labelRect, guiContent, false, false, false, false);
			list.EndLine();
		}

		public class ReadoutCache
		{
			public ReadoutCache(Listing_ResourceReadout list = null, int nestLevel = 0, Def def = null)
			{
				if (list != null) Update(list, nestLevel, def);
				if (def is ThingCategoryDef)
				{
					categories = ((ThingCategoryDef)def).childCategories?.Where(x => !x.treeNode?.catDef.resourceReadoutRoot ?? false)?.Select(y => y.treeNode)?.ToArray();
					things = new List<ReadoutCache>();
				}
			}
			public void Update(Listing_ResourceReadout list, int nestLevel, Def def = null, bool expanded = false)
			{
				if (def is ThingDef)
				{
					value = list.map.resourceCounter.GetCount(def as ThingDef);
					if (value == 0) return;
				}
				else if (def is ThingCategoryDef)
				{
					value = list.map.resourceCounter.GetCountIn(def as ThingCategoryDef);
					if (value == 0) return;

					buttonRect = new Rect(list.XAtIndentLevel(nestLevel), list.curY + list.lineHeight / 2f - 9f, 18f, 18f);
					controlID = GUIUtility.GetControlID(GUI.s_ButonHash, FocusType.Passive, buttonRect);					
				}
				//Printed label
				label = value.ToStringCached();
				
				//Container
				containerRect = new Rect(0f, list.curY, list.LabelWidth, list.lineHeight) { xMin = list.XAtIndentLevel(nestLevel) + 18f };

				//Highlight
				highlightRect = containerRect;
				highlightRect.width = 80f;
				highlightRect.yMax -= 3f;
				highlightRect.yMin += 3f;

				//Icon
				iconRect = new Rect(containerRect);
				iconRect.width = (iconRect.height = 28f);
				iconRect.y = containerRect.y + containerRect.height / 2f - iconRect.height / 2f;

				//Label
				labelRect = new Rect(containerRect) { xMin = iconRect.xMax + 6f };

				//Handle children defs
				if (expanded)
				{
					var thingsTmp = ((ThingCategoryDef)def).childThingDefs?.Where(x => x.PlayerAcquirable && list.map.resourceCounter.GetCount(x) > 0);
					foreach (var thing in thingsTmp)
					{
						ReadoutCache readout = things.FirstOrDefault(x => x.thingDef == thing);
						if (readout == null)
						{
							readout = new ReadoutCache(list, nestLevel + 1, thing);
							readout.thingDef = thing;
							things.Add(readout);
						}
						else readout.Update(list, nestLevel + 1, thing);
					}
				}
			}
			public List<ReadoutCache> things;
			public ThingDef thingDef;
			public int value;
			public string label;
			public Rect containerRect;
			public Rect highlightRect;
			public Rect iconRect;
			public Rect labelRect;
			public Rect buttonRect;
			public int controlID;
			public TreeNode_ThingCategory[] categories;
		}
	}
}
