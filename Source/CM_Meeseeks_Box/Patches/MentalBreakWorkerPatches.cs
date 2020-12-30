using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public static class MentalBreakWorkerPatches
    {
        [HarmonyPatch(typeof(MentalBreakWorker))]
        [HarmonyPatch("BreakCanOccur", MethodType.Normal)]
        public static class MentalBreakWorker_BreakCanOccur
        {
            private static List<string> disallowedMentalBreakNames = new List<string>
                { "FireStartingSpree", "Binging_DrugExtreme", "Jailbreaker", "Slaughterer", "RunWild", "GiveUpExit",
                  "Binging_DrugMajor", "BedroomTantrum", "SadisticRage", "CorpseObsession",
                  "Binging_Food", "Wander_OwnRoom"};

            [HarmonyPostfix]
            public static void Postfix(MentalBreakWorker __instance, ref bool __result, Pawn pawn)
            {
                if (__result == true && pawn.GetComp<CompMeeseeksMemory>() != null && disallowedMentalBreakNames.Contains(__instance.def.defName))
                {
                    __result = false;
                }
            }
        }
    }
}
