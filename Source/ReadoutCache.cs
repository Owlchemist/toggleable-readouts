using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using static ToggleableReadouts.ToggleableReadoutsUtility;
using static ToggleableReadouts.ModSettings_ToggleableReadouts;
 
namespace ToggleableReadouts
{
    public class ReadoutCache
	{
		public ReadoutCache(Listing_ResourceReadout list = null, int nestLevel = 0, Def def = null, int? count = null)
		{
			this.def = def;
			if (count != null)
			{
				value = (int)count;
				valueLabel = value.ToStringCached();
			}
			if (list != null) Update(list, nestLevel, def);
			if (def is ThingCategoryDef thingCategoryDef)
			{
				categories = thingCategoryDef.childCategories?.Where
					(x => (!x.treeNode?.catDef.resourceReadoutRoot ?? false) && !filteredDefs.Contains(x))?.
						OrderBy(x => !pinnedDefs.Contains(x)).Select(y => new ReadoutCache(null, nestLevel + 1, y)).ToArray();
				things = new List<ReadoutCache>();
			}
		}
		public void Update(Listing_ResourceReadout list, int nestLevel, Def def = null, bool expanded = false)
		{
			if (def is ThingDef thingDef)
			{
				value = list.map.resourceCounter.GetCount(thingDef);
				if (value == 0) return;
			}
			else if (def is ThingCategoryDef thingCategoryDef)
			{
				value = GetCountIn(thingCategoryDef, list);
				if (value == 0) return;

				buttonRect = new Rect(list.XAtIndentLevel(nestLevel), list.curY + list.lineHeight / 2f - 9f, 18f, 18f);
				controlID = GUIUtility.GetControlID(GUI.s_ButonHash, FocusType.Passive, buttonRect);					

				//Handle children defs
				if (expanded)
				{
					var childThingDefs = new List<ThingDef>(thingCategoryDef.childThingDefs);
					if (childThingDefs != null)
					{
						for (int i = childThingDefs.Count; i-- > 0;)
						{
							ThingDef thing = childThingDefs[i];
							if (!thing.PlayerAcquirable || 
								(list.map.resourceCounter.GetCount(thing) == 0 && !things.Any(y => y.def == thing)) || 
								filteredDefs.Contains(thing)) childThingDefs.Remove(thing);
						}
						childThingDefs.SortBy(x => !pinnedDefs.Contains(x));

						var length = childThingDefs.Count;
						for (int i = 0; i < length; i++)
						{
							var childThingDef = childThingDefs[i];
							ReadoutCache readout = things.FirstOrDefault(x => x.def == childThingDef);
							if (readout == null)
							{
								readout = new ReadoutCache(list, nestLevel + 1, childThingDef);
								readout.def = childThingDef;
								things.Add(readout);
							}
							else readout.Update(list, nestLevel + 1, childThingDef);
							numOfThings = things.Count;
						}
					}
				}
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

			//Label
			labelRect = new Rect(containerRect) { xMin = iconRect.xMax + 6f };
		}
		public List<ReadoutCache> things;
		public ReadoutCache[] categories;
		public Def def;
		public string valueLabel;
		public int value, controlID, numOfThings;
		public Rect containerRect, highlightRect, iconRect, labelRect, buttonRect;
	}
}