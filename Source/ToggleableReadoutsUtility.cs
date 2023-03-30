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
    [StaticConstructorOnStartup]
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
		public static bool mouseOver;
		static Listing_ResourceReadout _list = new Listing_ResourceReadout(null) { nestIndentWidth = 7f, lineHeight = 24f, verticalSpacing = 0 };

		static ToggleableReadoutsUtility()
		{
			filteredDefs = new HashSet<Def>();
			if (missingDefs == null) missingDefs = new HashSet<string>();
			if (pinnedDefs == null) pinnedDefs = new HashSet<Def>();
			if (exposedFilteredDefs == null) exposedFilteredDefs = new HashSet<string>();
			else
			{
				foreach (var entry in exposedFilteredDefs)
				{
					string defName = entry.Split('/')[1];
					string type = entry.Split('/')[0];
					bool pinned = !entry.Split('/').ElementAtOrDefault(2).NullOrEmpty();

					Def def = null;
					if (type == nameof(ThingDef)) def = DefDatabase<ThingDef>.GetNamed(defName, false);
					else if (type == nameof(ThingCategoryDef)) def = DefDatabase<ThingCategoryDef>.GetNamed(defName, false);

					if (def != null)
					{
						if (pinned) pinnedDefs.Add(def);
						else filteredDefs.Add(def);
					}
					else missingDefs.Add(entry); //Defs from a mod not currently loaded. Save it so it doesn't get erased
				}
			}

			BuildRootCache();
			
			guiStyle = Text.fontStyles[1];
		}
		public static void BuildRootCache()
		{
			var workingList = new List<ReadoutCache>();
			if (Prefs.ResourceReadoutCategorized)
			{
				var list = DefDatabase<ThingCategoryDef>.AllDefsListForReading;
				var length = list.Count;
				for (int i = 0; i < length; i++)
				{
					var def = list[i];
					if (def.resourceReadoutRoot && !filteredDefs.Contains(def)) 
					{
						if(pinnedDefs.Contains(def)) workingList.Insert(0, new ReadoutCache(null, 0, def));
						else workingList.Add(new ReadoutCache(null, 0, def));
					}
				}
			}
			else
			{
				var currentMap = Find.CurrentMap;
				if (currentMap != null)
				{
					foreach (var keyPair in currentMap.resourceCounter.countedAmounts)
					{
						var key = keyPair.Key;
						if ((keyPair.Value > 0 || key.resourceReadoutAlwaysShow) && !filteredDefs.Contains(key))
						{
							if (key == ThingDefOf.Silver || pinnedDefs.Contains(key)) workingList.Insert(0, new ReadoutCache(null, 0, key, keyPair.Value));
							else workingList.Add(new ReadoutCache(null, 0, key, keyPair.Value));
						}
					}
				}
			}
			readoutCache = workingList.ToArray();
		}
		public static void DoReadoutSimple(ResourceReadout list, Rect rect, float outRectHeight)
		{
			GUIClip.Internal_Push(rect, vector2zero, vector2zero, false);
			float currentLineY = 0f;

			//Cahe some things out of the loop
			eventCache = Event.current;
			eventType = eventCache.type;
			mouseOver = Mouse.IsOver(rect);
			guiStyle.alignment = TextAnchor.MiddleLeft;

			if (++ticker == 120)
			{
				BuildRootCache();
				ticker = 0;
			}
			
			foreach (ReadoutCache readOut in readoutCache)
			{
				
				Rect thingRect = new Rect(0f, currentLineY, 999f, 27f);
				if (thingRect.m_Height + thingRect.m_YMin >= list.scrollPosition.y && thingRect.m_YMin <= list.scrollPosition.y + outRectHeight)
				{
					DrawResourceSimple(thingRect, readOut);
				}
				currentLineY += 24f;
				
			}
			Text.Anchor = TextAnchor.UpperLeft;
			list.lastDrawnHeight = currentLineY;
			GUIClip.Internal_Pop();
		}
		static void DrawResourceSimple(Rect rect, ReadoutCache readOut)
		{
			ThingDef thingDef = readOut.def as ThingDef;
			//Make icon rect
			Rect iconRect = new Rect(rect) { m_Width = 27 };
			rect.m_XMin += 34;

			if (mouseOver && Mouse.IsOver(rect))
			{
				DrawTextureFast(rect, TexUI.HighlightTex, colorWhite);
				TooltipHandler.TipRegion(rect, new TipSignal(() => thingDef.LabelCap + ": " + thingDef.description.CapitalizeFirst(), (int)thingDef.shortHash));
				HandleClicks(eventCache, eventType, rect, thingDef);
			}
			DrawTextureFast(iconRect, thingDef.uiIcon, thingDef.graphicData.color);

			guiContent.m_Text = readOut.valueLabel;
			guiStyle.Internal_Draw_Injected(ref rect, guiContent, false, false, false, false);
		}
		public static void DoReadoutCategorized(ResourceReadout instance, Rect rect)
		{
			eventCache = Event.current;
			eventType = eventCache.type;

			if (updateNow = ++ticker == 120) ticker = 0;

			Listing_ResourceReadout list = _list;
			list.listingRect = rect;
			list.columnWidthInt = list.listingRect.m_Width;
			list.curX = list.curY = list.maxHeightColumnSeen = 0f;
			GUIClip.Internal_Push(rect, vector2zero, vector2zero, false);

			list.map = Current.gameInt.maps[(int)Current.gameInt.currentMapIndex];

			mouseOver = Mouse.IsOver(rect);
			guiStyle.alignment = TextAnchor.MiddleLeft;
			for (int i = 0; i < readoutCache.Length; i++) DoCategory(list, readoutCache[i], 0, 32);
			
			GUIClip.Internal_Pop();
			instance.lastDrawnHeight = Math.Max(list.CurHeight, list.maxHeightColumnSeen);
		}
		public static void DoCategory(Listing_ResourceReadout list, ReadoutCache readout, int nestLevel, int openMask)
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
			guiStyle.Internal_Draw_Injected(ref readout.labelRect, guiContent, false, false, false, false);

			//Draw children
			list.EndLine();
			if (expanded && readout.things != null) DoCategoryChildren(readout, list, nestLevel + 1, openMask);
		}
		static void DoCategoryChildren(ReadoutCache readout, Listing_ResourceReadout list, int indentLevel, int openMask)
		{
			try
			{
				foreach (var entry in readout.categories)
				{
					DoCategory(list, entry, indentLevel, openMask);
				}

				for (int i = 0; i < readout.numOfThings; i++)
				{
					ReadoutCache entry = readout.things[i];
					if (entry.value != 0 ) DoThingDef(entry, list);
				}
			}
			//The collections getting oughta sync is not expected to happen, but some mods could throw a few curveballs
			catch (System.IndexOutOfRangeException)
			{
				ticker = 119;
				updateNow = true;
				return;
			}
		}
		static void DoThingDef(ReadoutCache readout, Listing_ResourceReadout list)
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
			yield return new FloatMenuOption("ToggleableReadouts.Filter".Translate(def.label), () => FilterDef(def), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

			//Add pinning opion
			string label = pinnedDefs.Contains(def) ? "ToggleableReadouts.Unpin".Translate(def.label) : "ToggleableReadouts.Pin".Translate(def.label);
			yield return new FloatMenuOption(label, () => PinDef(def), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);

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
						entry.numOfThings -= entry.things?.RemoveAll(x => x.def == def) ?? 0;
						entry.categories?.ToList().ForEach(x => x.numOfThings -= x.things.RemoveAll(x => x.def == def));
					}
				}
				else if (def is ThingCategoryDef)
				{
					readoutCache = readoutCache.Where(x => x.def != def).ToArray();
					foreach (var entry in readoutCache)
					{
						entry.categories = entry.categories?.Where(x => x.def != def).ToArray();
					}
				}
				ticker = 119;
			}
			LoadedModManager.GetMod<Mod_ToggleableReadouts>().WriteSettings();
		}
		static void PinDef(Def def)
		{
			if (pinnedDefs.Contains(def)) pinnedDefs.Remove(def);
			else pinnedDefs.Add(def);
			BuildRootCache();
			if (Prefs.ResourceReadoutCategorized)
			{
				readoutCache.ToList().ForEach
				(x => 
					{
						x.things.OrderBy(y => !pinnedDefs.Contains(y.def));
						x.categories.OrderBy(y => !pinnedDefs.Contains(y.def));
					}
				);
			}
			updateNow = true;
			ticker = 119;
			LoadedModManager.GetMod<Mod_ToggleableReadouts>().WriteSettings();
		}
		public static int GetCountIn(ThingCategoryDef cat, Listing_ResourceReadout list)
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
	}
}
