using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public static class FloatMenuMakerMapPatches
    {
        //[HarmonyPatch(typeof(FloatMenuMakerMap))]
        //[HarmonyPatch("AddJobGiverWorkOrders_NewTmp", MethodType.Normal)]
        //static class FloatMenuMakerMap_AddJobGiverWorkOrders_NewTmp
        //{
        //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        //    {
        //        List<CodeInstruction> list = instructions.ToList();

        //        MethodInfo addFloatMenuOption = SymbolExtensions.GetMethodInfo(() => new List<FloatMenuOption>().Add(default));

        //        int nextAddFloatMenuOptionIndex = list.FindIndex(0, (instruction => (instruction.Calls(addFloatMenuOption))));
        //        int foundInstructions = 0;

        //        while (nextAddFloatMenuOptionIndex >= 0 && nextAddFloatMenuOptionIndex < list.Count)
        //        {
        //            ++foundInstructions;
        //            ++nextAddFloatMenuOptionIndex;
        //            nextAddFloatMenuOptionIndex = list.FindIndex(nextAddFloatMenuOptionIndex, (instruction => (instruction.Calls(addFloatMenuOption))));
        //        }

        //        Log.Warning("Found " + foundInstructions.ToString() + " attempts to add FloatMenuOptions in AddJobGiverWorkOrders_NewTmp");

        //        foreach (CodeInstruction instruction in list)
        //            yield return instruction;
        //    }
        //}
    }
}
