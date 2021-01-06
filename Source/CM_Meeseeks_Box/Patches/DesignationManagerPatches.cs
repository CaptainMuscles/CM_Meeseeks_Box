using System;

using HarmonyLib;
using Verse;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public static class DesignationManagerPatches
    {
        [HarmonyPatch(typeof(DesignationManager))]
        [HarmonyPatch("DesignationOn", new Type[] { typeof(Thing), typeof(DesignationDef) })]
        public class DesignationManager_DesignationOn
        {
            public static bool getFudged = false;

            [HarmonyPostfix]
            public static void Postfix(Thing t, DesignationDef def, ref Designation __result)
            {
                if (__result == null && getFudged)
                {
                    __result = new Designation(t, def);
                }
            }
        }
    }
}
