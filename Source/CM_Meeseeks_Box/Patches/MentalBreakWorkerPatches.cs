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

            // Maybe just allow these two for now
            private static List<string> allowedMentalBreakNames = new List<string>
            { "CM_Meeseeks_Box_MentalBreak_MeeseeksKillCreator", "CM_Meeseeks_Box_MentalBreak_MeeseeksMakeMeeseeks" };

            [HarmonyPostfix]
            public static void Postfix(MentalBreakWorker __instance, ref bool __result, Pawn pawn)
            {
                if (__result == true && pawn.GetComp<CompMeeseeksMemory>() != null && !allowedMentalBreakNames.Contains(__instance.def.defName))
                {
                    __result = false;
                }
            }
        }
    }
}
