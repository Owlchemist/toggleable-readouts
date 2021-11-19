
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using static ToggleableReadouts.ToggleableReadoutsUtility;

namespace ToggleableReadouts
{
	//...
	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.DoReadoutCategorized))]
	public class Patch_DoReadoutCategorized
	{
		static public bool Prefix(ResourceReadout __instance, Rect rect)
		{
			DoReadoutCategorized(__instance, rect);
			return false;
		}
    }
}
