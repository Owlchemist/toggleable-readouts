
using HarmonyLib;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static ToggleableReadouts.ToggleableReadoutsUtility;

namespace ToggleableReadouts
{
	//Replaces the categorized rendering to our own
	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.DoReadoutCategorized))]
	[HarmonyPriority(Priority.Last)]
	class Replace_DoReadoutCategorized
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToggleableReadoutsUtility), nameof(DoReadoutCategorized)));
			yield return new CodeInstruction(OpCodes.Ret);
		}
    }

	//Replaces the simple rendering to our own
	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.DoReadoutSimple))]
	[HarmonyPriority(Priority.Last)]
	class Replace_DoReadoutSimple
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Ldarg_2);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToggleableReadoutsUtility), nameof(DoReadoutSimple)));
			yield return new CodeInstruction(OpCodes.Ret);
		}
    }

	[HarmonyPatch]
    class ResetCache
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(World), nameof(World.FinalizeInit));
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
