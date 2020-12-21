﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public static class MeeseeksCommandPatches
    {
        [HarmonyPatch(typeof(PawnGenerator))]
        [HarmonyPatch("GenerateTraits", MethodType.Normal)]
        public static class MeeseeksGenerateTraits
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn)
            {
                if (pawn.kindDef == MeeseeksDefOf.MeeseeksKind)
                {
                    TraitDef meeseeksTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CM_Meeseeks_Box_Trait_Meeseeks");
                    if (meeseeksTrait != null)
                        pawn.story.traits.GainTrait(new Trait(meeseeksTrait, 0, true));
                    TraitDef annoyingVoice = DefDatabase<TraitDef>.GetNamedSilentFail("AnnoyingVoice");
                    if (annoyingVoice != null)
                        pawn.story.traits.GainTrait(new Trait(annoyingVoice, 0, true));
                    return false;
                }

                return true;
            }
        }

        //[HarmonyPatch(typeof(Pawn_NeedsTracker))]
        //[HarmonyPatch("ShouldHaveNeed", MethodType.Normal)]
        //public static class MeeseeksNeeds
        //{
        //    [HarmonyPrefix]
        //    public static bool Listener(NeedDef nd, ref bool __result, Pawn ___pawn)
        //    {
        //        if (___pawn.kindDef == MeeseeksDefOf.MeeseeksKind)
        //        {
        //            // Allowing joy for now because there are errors if we don't - need more intricate patch to fix that
        //            __result = (nd.defName == "Mood" || nd.defName == "Joy");
        //            return false;
        //        }

        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch("StartJob", MethodType.Normal)]
        public static class MeeseeksStartJob
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn_JobTracker __instance)
            {
                //Logger.MessageFormat(__instance, "StartJob Prefix");
            }

            [HarmonyPostfix]
            public static void MonitorMeeseeksJob(Pawn_JobTracker __instance, Job newJob, Pawn ___pawn)
            {
                //Logger.MessageFormat(__instance, "StartJob Postfix");

                if (___pawn != null)
                { 
                    CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                        compMeeseeksMemory.StartedJob(__instance.curJob);
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch("EndCurrentJob", MethodType.Normal)]
        public static class MeeseeksEndCurrentJob
        {
            [HarmonyPrefix]
            public static void Prefix(Pawn_JobTracker __instance, JobCondition condition, Pawn ___pawn)
            {
                //Logger.MessageFormat(__instance, "EndCurrentJob Prefix");

                if (___pawn != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                        compMeeseeksMemory.EndCurrentJob(__instance.curJob, __instance.curDriver, condition);
                }
            }

            [HarmonyPostfix]
            public static void MonitorMeeseeksJob(Pawn_JobTracker __instance, JobCondition condition)
            {
                //Logger.MessageFormat(__instance, "EndCurrentJob Postfix");
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch("TryTakeOrderedJob", MethodType.Normal)]
        public static class MeeseeksTryTakeOrderedJob
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn_JobTracker __instance, Job job, ref bool __result, Pawn ___pawn)
            {
                //Logger.MessageFormat(__instance, "TryTakeOrderedJob Prefix");

                if (___pawn != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        bool canTakeJob = compMeeseeksMemory.CanTakeOrderedJob(job);

                        //Logger.MessageFormat(__instance, "TryTakeOrderedJob Meeseeks canTakeJob: {0}", canTakeJob.ToString());

                        if (!canTakeJob)
                        {
                            __result = false;
                            return false;
                        }
                        else
                        {
                            compMeeseeksMemory.PreTryTakeOrderedJob(job);
                        }
                    }
                }

                return true;
            }

            [HarmonyPostfix]
            public static void MonitorMeeseeksJob(Pawn_JobTracker __instance, Job job, ref bool __result, Pawn ___pawn)
            {
                Logger.MessageFormat(__instance, "TryTakeOrderedJob Postfix");

                if (__result == true && ___pawn != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        Logger.MessageFormat(__instance, "Meeseeks TookOrderedJob");
                        compMeeseeksMemory.PostTryTakeOrderedJob(__result, job);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("CanTakeOrder", MethodType.Normal)]
        public static class MeeseeksCanTakeOrder
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn, ref bool __result)
            {
                //Logger.MessageFormat(pawn, "CanTakeOrder Prefix");

                if (pawn != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        bool canTakeOrder = compMeeseeksMemory.CanTakeOrders();

                        //Logger.MessageFormat(pawn, "CanTakeOrder Meeseeks: {0}", canTakeOrder.ToString());

                        if (!canTakeOrder)
                        {
                            __result = false;
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("GetGizmos", MethodType.Normal)]
        public static class MeeseeksNoGizmosAfterJobStarted
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn __instance, ref IEnumerable<Gizmo> __result)
            {
                //Logger.MessageFormat(__instance, "GetGizmos Prefix");

                if (__instance != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = __instance.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        bool givenTask = compMeeseeksMemory.GivenTask();

                        //Logger.MessageFormat(__instance, "CanTakeOrder Meeseeks: {0}", canTakeOrder.ToString());

                        if (givenTask)
                        {
                            if (__result == null)
                                __result = new List<Gizmo>();
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Need))]
        [HarmonyPatch("IsFrozen", MethodType.Getter)]
        public static class MeeseeksFreezeNeeds
        {
            [HarmonyPrefix]
            public static bool Prefix(Need __instance, ref bool __result, Pawn ___pawn)
            {
                if (___pawn.kindDef == MeeseeksDefOf.MeeseeksKind && __instance.def.defName != "Mood")
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
        [HarmonyPatch("ApparelScoreGain_NewTmp", MethodType.Normal)]
        public static class JobGiver_OptimizeApparelApparelScoreGain_NewTmp_MeeseeksSeekJobAppropriateClothing
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn pawn, Apparel ap, ref float __result)
            {
                if (pawn.kindDef == MeeseeksDefOf.MeeseeksKind)
                {
                    //Logger.MessageFormat(pawn, "Meeseeks checking apparel...");

                    bool workRelevant = false;
                    CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null && compMeeseeksMemory.savedJob != null && compMeeseeksMemory.savedJob.workGiverDef != null)
                    {
                        //Logger.MessageFormat(pawn, "Found workGiverDef...");
                        List<SkillDef> relevantSkills = compMeeseeksMemory.savedJob.workGiverDef.workType.relevantSkills;
                        List<StatModifier> statModifiers = ap.def.equippedStatOffsets;
                        if (relevantSkills != null && statModifiers != null)
                        {
                            //Logger.MessageFormat(pawn, "Found relevantSkills...");
                            foreach (StatModifier statModifier in statModifiers)
                            {
                                List<SkillNeed> skillNeedOffsets = statModifier.stat.skillNeedOffsets;
                                List<SkillNeed> skillNeedFactors = statModifier.stat.skillNeedFactors;

                                if (skillNeedOffsets != null)
                                {
                                    //Logger.MessageFormat(pawn, "Found skillNeedOffsets...");
                                    foreach (SkillNeed skillNeed in skillNeedOffsets)
                                    {
                                        if (relevantSkills.Contains(skillNeed.skill))
                                        {
                                            //Logger.MessageFormat(pawn, "Relevant!");
                                            workRelevant = true;
                                            break;
                                        }
                                    }
                                }

                                if (!workRelevant && skillNeedFactors != null)
                                {
                                    foreach (SkillNeed skillNeed in skillNeedFactors)
                                    {
                                        if (relevantSkills.Contains(skillNeed.skill))
                                        {
                                            workRelevant = true;
                                            break;
                                        }
                                    }
                                }

                                if (workRelevant)
                                    break;
                            }
                        }
                    }

                    if (!workRelevant)
                    {
                        __result = -1000.0f;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_DraftController))]
        [HarmonyPatch("Drafted", MethodType.Setter)]
        public static class Pawn_DraftController_Drafted_CantUndraftMeeseeks
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn_DraftController __instance, bool ___draftedInt)
            {
                //Logger.MessageFormat(__instance, "Drafted Prefix");

                if (__instance.pawn != null && ___draftedInt)
                {
                    CompMeeseeksMemory compMeeseeksMemory = __instance.pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_HunterHunt))]
        [HarmonyPatch("ShouldSkip", MethodType.Normal)]
        public static class WorkGiver_HunterHunt_ShouldSkip_MeeseeksCanHuntWithoutRequirements
        {
            [HarmonyPrefix]
            public static bool Prefix(WorkGiver_HunterHunt __instance, Pawn pawn, ref bool __result)
            {
                //Logger.MessageFormat(__instance, "Prefix");

                if (pawn != null)
                {
                    CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

                    if (compMeeseeksMemory != null)
                    {
                        __result = false;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Apparel))]
        [HarmonyPatch("Notify_PawnKilled", MethodType.Normal)]
        public class Apparel_Notify_PawnKilled_MeeseeksClothesDontTaint
        {
            [HarmonyPostfix]
            public static void Postfix(Apparel __instance, ref bool ___wornByCorpseInt)
            {
                Pawn pawn = __instance.Wearer;

                if (pawn != null && pawn.kindDef == MeeseeksDefOf.MeeseeksKind)
                {
                    ___wornByCorpseInt = false;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_WorkSettings))]
        [HarmonyPatch("GetPriority", MethodType.Normal)]
        public static class Pawn_WorkSettings_GetPriority_IgnorePriorityChanges
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn_WorkSettings __instance, ref int __result, Pawn ___pawn)
            {
                if (___pawn.kindDef == MeeseeksDefOf.MeeseeksKind)
                {
                    __result = Pawn_WorkSettings.DefaultPriority;
                    Logger.MessageFormat(__instance, "Forcing default work priority of: {0}", __result);
                }
            }
        }

        //[HarmonyPatch(typeof(PriorityWork))]
        //[HarmonyPatch("GetGizmos", MethodType.Normal)]
        //public static class MeeseeksCantClearPrioritizedWork
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(PriorityWork __instance, Pawn ___pawn, ref IEnumerable<Gizmo> __result)
        //    {
        //        //Logger.MessageFormat(__instance, "GetGizmos Prefix");

        //        if (___pawn != null)
        //        {
        //            CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

        //            if (compMeeseeksMemory != null)
        //            {
        //                bool canTakeOrder = compMeeseeksMemory.CanTakeOrders();

        //                Logger.MessageFormat(__instance, "CanTakeOrder Meeseeks: {0}", canTakeOrder.ToString());

        //                if (!canTakeOrder)
        //                {
        //                    if (__result == null)
        //                        __result = new List<Gizmo>();
        //                    return false;
        //                }
        //            }
        //        }

        //        return true;
        //    }
        //}

        //[HarmonyPatch(typeof(PriorityWork))]
        //[HarmonyPatch("ClearPrioritizedWorkAndJobQueue", MethodType.Normal)]
        //public static class MeeseeksWontClearPrioritizedWork
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix(PriorityWork __instance, Pawn ___pawn)
        //    {
        //        //Logger.MessageFormat(__instance, "ClearPrioritizedWorkAndJobQueue Prefix");

        //        if (___pawn != null)
        //        {
        //            CompMeeseeksMemory compMeeseeksMemory = ___pawn.GetComp<CompMeeseeksMemory>();

        //            if (compMeeseeksMemory != null)
        //            {
        //                bool canTakeOrder = compMeeseeksMemory.CanTakeOrders();

        //                Logger.MessageFormat(___pawn, "ClearPrioritizedWorkAndJobQueue Meeseeks: {0}", canTakeOrder.ToString());

        //                if (!canTakeOrder)
        //                {
        //                    return false;
        //                }
        //            }
        //        }

        //        return true;
        //    }
        //}
    }
}
