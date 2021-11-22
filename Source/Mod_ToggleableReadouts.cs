using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static ToggleableReadouts.ToggleableReadoutsUtility;
using static ToggleableReadouts.ModSettings_ToggleableReadouts;
 
namespace ToggleableReadouts
{
    public class Mod_ToggleableReadouts : Mod
	{
		public Mod_ToggleableReadouts(ModContentPack content) : base(content)
		{
			new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
			base.GetSettings<ModSettings_ToggleableReadouts>();
			LongEventHandler.QueueLongEvent(() => Setup(), null, false, null);
		}
		
		public override void DoSettingsWindowContents(Rect inRect)
		{
			inRect.yMin += 20f;
			inRect.yMax -= 20f;
			Listing_Standard options = new Listing_Standard();
			Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
			Rect rect = new Rect(0f, 0f, inRect.width - 30f, (filteredDefs.Count + 4) * 36f);
			Widgets.BeginScrollView(outRect, ref scrollPos, rect, true);
			options.Begin(rect);

				options.Label("ToggleableReadouts.Settings.Help".Translate());
				if (Prefs.DevMode) options.CheckboxLabeled("DevMode: Disable mod", ref disableMod, null);
				options.GapLine();
				options.Gap();
				for (int i = 0; i < filteredDefs.Count; ++i)
				{
					Def def = filteredDefs.ElementAt(i);
					string defType = "ToggleableReadouts.Settings.Thing".Translate();
					if (def is ThingCategoryDef) defType = "ToggleableReadouts.Settings.ThingCategoryDef".Translate();
					
					if (def != null && options.ButtonText(def.label + " (" + defType + ")"))
					{
						filteredDefs.Remove(def);
						ticker = 119;
						BuildRootCache();
						break;
					}
				}

				options.End();
			Widgets.EndScrollView();
			
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Toggleable Readouts";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
		}
		
	}
	
	public class ModSettings_ToggleableReadouts : ModSettings
	{
		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				exposedFilteredDefs.Clear();
				
				exposedFilteredDefs.AddRange(filteredDefs.Select(def => (def?.GetType()?.Name + "/" + def.defName)));
				exposedFilteredDefs.AddRange(missingDefs);
			}

			Scribe_Collections.Look(ref exposedFilteredDefs, "exposedFilteredDefs", LookMode.Value);
			base.ExposeData();
		}
		public static HashSet<string> exposedFilteredDefs;
		public static HashSet<string> missingDefs;
		public static HashSet<Def> filteredDefs;
		public static Vector2 scrollPos;
		public static bool disableMod;
	}
}
