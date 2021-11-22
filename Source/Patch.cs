
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using static ToggleableReadouts.ToggleableReadoutsUtility;
using static ToggleableReadouts.ModSettings_ToggleableReadouts;

namespace ToggleableReadouts
{
	//Replaces the categorized rendering to our own
	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.DoReadoutCategorized))]
	public class Patch_DoReadoutCategorized
	{
		static public bool Prefix(ResourceReadout __instance, Rect rect)
		{
			if (!disableMod) DoReadoutCategorized(__instance, rect);
			return disableMod;
		}
    }

	//Replaces the simple rendering to our own
	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.DoReadoutSimple))]
	public class Patch_DoReadoutSimple
	{
		static public bool Prefix(ResourceReadout __instance, Rect rect, float outRectHeight)
		{
			if (!disableMod) DoReadoutSimple(__instance, rect, outRectHeight);
			return disableMod;
		}
    }

	[HarmonyPatch]
    class ResetCache
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Game), nameof(Game.LoadGame));
            yield return AccessTools.Method(typeof(Game), nameof(Game.InitNewGame));
			yield return AccessTools.PropertySetter(typeof(Prefs), nameof(Prefs.ResourceReadoutCategorized));
        }

        static public void Postfix()
		{
			ticker = 119;
			updateNow = true;
			BuildRootCache();
		}
    }
}
