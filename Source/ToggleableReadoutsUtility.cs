using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using static ToggleableReadouts.FastGUI;
using static ToggleableReadouts.ResourceBank;
using static ToggleableReadouts.ModSettings_ToggleableReadouts;
 
namespace ToggleableReadouts
{
    internal static class ToggleableReadoutsUtility
	{
		//Primary
		static ReadoutCache[] readoutCache;
		public static bool updateNow = true;
		public static int ticker = 119;

		//Cache
		public static Event eventCache;
		public static EventType eventType;
		static GUIContent guiContent = GUIContent.Temp("");
		static GUIStyle guiStyle;
		static int numOfCategories;
		public static bool mouseOver;
		static Listing_ResourceReadout list;

		public static void Setup()
		{
			filteredDefs = new HashSet<Def>();
			if (exposedFilteredDefs == null) exposedFilteredDefs = new HashSet<string>();
			else
			{
				foreach (var entry in exposedFilteredDefs)
				{
					string defName = entry.Split('/')[1];
					string type = entry.Split('/')[0];

					if (type == nameof(ThingDef)) filteredDefs.Add(DefDatabase<ThingDef>.GetNamed(defName, true));
					else if (type == nameof(ThingCategoryDef)) filteredDefs.Add(DefDatabase<ThingCategoryDef>.GetNamed(defName, true));
				}
			}

			BuildRootCache();
			
			guiStyle = Text.fontStyles[1];
			list = new Listing_ResourceReadout(null);
			list.nestIndentWidth = 7f;
			list.lineHeight = 24f;
			list.verticalSpacing = 0f;
		}

		public static void BuildRootCache()
		{
			readoutCache = DefDatabase<ThingCategoryDef>.AllDefsListForReading.Where(x => x.resourceReadoutRoot && !filteredDefs.Contains(x)).Select(x => new ReadoutCache(null, 0, x)).ToArray();
			numOfCategories = readoutCache.Length;
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
				DoCategoryFast(list, readoutCache[i], 0, 32);
			}
			
			GUIClip.Internal_Pop();
			instance.lastDrawnHeight = Math.Max(list.CurHeight, list.maxHeightColumnSeen);
		}

		public static void DoCategoryFast(Listing_ResourceReadout list, ReadoutCache readout, int nestLevel, int openMask)
		{
			TreeNode_ThingCategory node = ((ThingCategoryDef)readout.def).treeNode;

			//Category expanded?
			bool expanded = (node.openBits & openMask) != 0;

			//Fetch or build the cache
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
				HandleClicks(eventCache, eventType, readout.containerRect, node.catDef);
			}
			
			//Draw icon
			DrawTextureFast(readout.iconRect, node.catDef.icon, colorWhite);

			//Draw label
			guiContent.m_Text = readout.valueLabel;
			guiStyle.alignment = TextAnchor.MiddleLeft;
			guiStyle.Internal_Draw_Injected(ref readout.labelRect, guiContent, false, false, false, false);

			//Draw children
			list.EndLine();
			if (expanded && readout.things != null) DoCategoryChildrenFast(readout, list, nestLevel + 1, openMask);
		}

		static void DoCategoryChildrenFast(ReadoutCache readout, Listing_ResourceReadout list, int indentLevel, int openMask)
		{
			for (int i = 0; i < readout.numOfCategories; ++i)
			{
				DoCategoryFast(list, readout.categories[i], indentLevel, openMask);
			}

			for (int i = 0; i < readout.numOfThings; i++)
			{
				ReadoutCache entry = readout.things[i];
				if (entry.value != 0 ) DoThingDefFast(entry, list, indentLevel + 1);
			}
		}

		static void DoThingDefFast(ReadoutCache readout, Listing_ResourceReadout list, int nestLevel)
		{
			ThingDef thingDef = readout.def as ThingDef;

			//Compensate curY
			readout.containerRect.m_YMin = readout.highlightRect.m_YMin = readout.labelRect.m_YMin = list.curY;
			readout.iconRect.m_YMin = list.curY - 2;
			
			if (mouseOver && Mouse.IsOver(readout.containerRect))
			{
				DrawTextureFast(readout.containerRect, TexUI.HighlightTex, colorWhite);
				TooltipHandler.TipRegion(readout.containerRect, new TipSignal(() => thingDef.LabelCap + ": " + thingDef.description.CapitalizeFirst(), (int)thingDef.shortHash));
				HandleClicks(eventCache, eventType, readout.containerRect, thingDef);
			}
			DrawTextureFast(readout.iconRect, thingDef.uiIcon, thingDef.graphicData.color);

			guiContent.m_Text = readout.valueLabel;
			guiStyle.Internal_Draw_Injected(ref readout.labelRect, guiContent, false, false, false, false);
			list.EndLine();
		}

		static void HandleClicks(Event eventCurrent, EventType eventType, Rect rect, Def def)
		{
			int mouseButton = eventCurrent.button;
			if (mouseButton == 1)
			{
				if (eventType == EventType.MouseDown) eventCurrent.Use();
				else if (eventType == EventType.MouseUp)
				{
					List<FloatMenuOption> righClickMenu = HandleRightClick(def).ToList<FloatMenuOption>();
					if (righClickMenu.Count != 0)
					{
						Find.WindowStack.Add(new FloatMenu(righClickMenu));
						eventCurrent.Use();
					}
				}
			}
		}

		static IEnumerable<FloatMenuOption> HandleRightClick(Def def)
		{	
			//Add option to make this pawn a group leader
			yield return new FloatMenuOption("ToggleableReadouts.Filter".Translate(def.label), delegate()
			{
				FilterDef(def);
			}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

			yield break;
		}

		static void FilterDef(Def def)
		{
			if (filteredDefs.Add(def))
			{
				Messages.Message("ToggleableReadouts.FilterApplied".Translate(def.label), MessageTypeDefOf.NeutralEvent, false);
				if (def is ThingDef)
				{
					foreach (var entry in readoutCache)
					{
						entry.numOfThings -= entry.things.RemoveAll(x => x.def == def);
						entry.categories?.ToList().ForEach(x => x.numOfThings -= x.things.RemoveAll(x => x.def == def));
					}
				}
				else if (def is ThingCategoryDef)
				{
					readoutCache = readoutCache.Where(x => x.def != def).ToArray();
					numOfCategories = readoutCache.Length;
					foreach (var entry in readoutCache)
					{
						entry.categories = entry.categories?.Where(x => x.def != def).ToArray();
					}
				}
				ticker = 119;
			}
			LoadedModManager.GetMod<Mod_ToggleableReadouts>().WriteSettings();
		}

		static int GetCountIn(ThingCategoryDef cat, Listing_ResourceReadout list)
		{
			int num = 0;
			for (int i = 0; i < cat.childThingDefs.Count; ++i)
			{
				var def = cat.childThingDefs[i];
				if (filteredDefs.Contains(def)) continue;
				num += list.map.resourceCounter.GetCount(cat.childThingDefs[i]);
			}
			for (int j = 0; j < cat.childCategories.Count; ++j)
			{
				if (!cat.childCategories[j].resourceReadoutRoot)
				{
					var def = cat.childCategories[j];
					if (filteredDefs.Contains(def)) continue;
					num += GetCountIn(cat.childCategories[j], list);
				}
			}
			return num;
		}

		public class ReadoutCache
		{
			public ReadoutCache(Listing_ResourceReadout list = null, int nestLevel = 0, Def def = null)
			{
				this.def = def;
				if (list != null) Update(list, nestLevel, def);
				if (def is ThingCategoryDef)
				{
					categories = ((ThingCategoryDef)def).childCategories?.Where
						(x => (!x.treeNode?.catDef.resourceReadoutRoot ?? false) && !filteredDefs.Contains(x))?.Select
							(y => new ReadoutCache(null, nestLevel + 1, y)).ToArray();
					numOfCategories = categories.Length;
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
					value = GetCountIn(def as ThingCategoryDef, list);
					if (value == 0) return;

					buttonRect = new Rect(list.XAtIndentLevel(nestLevel), list.curY + list.lineHeight / 2f - 9f, 18f, 18f);
					controlID = GUIUtility.GetControlID(GUI.s_ButonHash, FocusType.Passive, buttonRect);					
				}
				//Printed label
				valueLabel = value.ToStringCached();
				
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
					IEnumerable<ThingDef> childDefs = ((ThingCategoryDef)def).childThingDefs?.Where
						(x => x.PlayerAcquirable && (list.map.resourceCounter.GetCount(x) > 0 || things.Any(y => y.def == x)) && !filteredDefs.Contains(x));
					foreach (var thing in childDefs)
					{
						ReadoutCache readout = things.FirstOrDefault(x => x.def == thing);
						if (readout == null)
						{
							readout = new ReadoutCache(list, nestLevel + 1, thing);
							readout.def = thing;
							things.Add(readout);
						}
						else readout.Update(list, nestLevel + 1, thing);
						numOfThings = things.Count;
					}
				}
			}
			public List<ReadoutCache> things;
			public ReadoutCache[] categories;
			public Def def;
			public string valueLabel;
			public int value, controlID, numOfThings, numOfCategories;
			public Rect containerRect, highlightRect, iconRect, labelRect, buttonRect;
		}
	}
}
