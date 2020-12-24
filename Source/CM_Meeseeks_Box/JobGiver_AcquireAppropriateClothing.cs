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
    public class JobGiver_AcquireAppropriateClothing : ThinkNode_JobGiver
    {
        private static NeededWarmth neededWarmth;

        private static StringBuilder debugSb;

        private const int ApparelOptimizeCheckIntervalMin = 6000;

        private const int ApparelOptimizeCheckIntervalMax = 9000;

        private const float MinScoreGainToCare = 0.05f;

        private const float ScoreFactorIfNotReplacing = 10f;

        private static readonly SimpleCurve InsulationColdScoreFactorCurve_NeedWarm = new SimpleCurve
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(30f, 8f)
        };

        private static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.2f, 0.2f),
            new CurvePoint(0.22f, 0.6f),
            new CurvePoint(0.5f, 0.6f),
            new CurvePoint(0.52f, 1f)
        };

        private static readonly List<BodyPartGroupDef> clothingPartPriority = new List<BodyPartGroupDef>
        {
            BodyPartGroupDefOf.FullHead,
            BodyPartGroupDefOf.UpperHead,
            BodyPartGroupDefOf.Eyes,
            BodyPartGroupDefOf.Torso,
            BodyPartGroupDefOf.Legs
        };

        private static HashSet<BodyPartGroupDef> tmpBodyPartGroupsWithRequirement = new HashSet<BodyPartGroupDef>();

        private static HashSet<ThingDef> tmpAllowedApparels = new HashSet<ThingDef>();

        private static HashSet<ThingDef> tmpRequiredApparels = new HashSet<ThingDef>();

        private void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + Rand.Range(6000, 9000);
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AcquireAppropriateClothing obj = (JobGiver_AcquireAppropriateClothing)base.DeepCopy(resolve);
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(string.Concat(pawn, " tried to run JobGiver_OptimizeApparel without an OutfitTracker"), 5643897);
                return null;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce(string.Concat("Non-colonist ", pawn, " tried to optimize apparel."), 764323);
                return null;
            }
            if (pawn.IsQuestLodger())
            {
                return null;
            }

            // Meeseeks only
            CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();
            if (compMeeseeksMemory == null || compMeeseeksMemory.savedJob == null || compMeeseeksMemory.savedJob.targetA == null || !compMeeseeksMemory.savedJob.targetA.IsValid)
            {
                if (compMeeseeksMemory != null && compMeeseeksMemory.savedJob == null)
                    Logger.MessageFormat(this, "No job?");
                if (compMeeseeksMemory != null && compMeeseeksMemory.savedJob != null)
                {
                    if (compMeeseeksMemory.savedJob.targetA == null)
                        Logger.MessageFormat(this, "No target?");
                    else if (!compMeeseeksMemory.savedJob.targetA.IsValid)
                        Logger.MessageFormat(this, "Target not valid?");
                }
                return null;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    Logger.MessageFormat(this, "Not time to change clothes!");
                    return null;
                }
            }
            else
            {
                debugSb = new StringBuilder();
                debugSb.AppendLine(string.Concat("JobGiver_AcquireAppropriateClothing - Scanning for ", pawn, " at ", pawn.Position));
            }



            List<SkillDef> relevantSkills = null;
            if (compMeeseeksMemory.savedJob.workGiverDef == null)
                relevantSkills = new List<SkillDef>();
            else
                relevantSkills = compMeeseeksMemory.savedJob.workGiverDef.workType.relevantSkills;

            // For some reason the entry for melee doesn't seem to get read for the kill job, so we'll do it manually...
            if (compMeeseeksMemory.savedJob.def == JobDefOf.AttackMelee || compMeeseeksMemory.savedJob.def == MeeseeksDefOf.CM_Meeseeks_Box_Job_Kill || pawn.Drafted)
                relevantSkills.Add(SkillDefOf.Melee);
            if (compMeeseeksMemory.savedJob.def == JobDefOf.AttackStatic || compMeeseeksMemory.savedJob.def == MeeseeksDefOf.CM_Meeseeks_Box_Job_Kill || pawn.Drafted)
                relevantSkills.Add(SkillDefOf.Shooting);

            List<Thing> availableApparel = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).Where(t => CanWearThis(pawn, t, relevantSkills)).ToList();
            if (availableApparel.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }

            bool combatJob = (compMeeseeksMemory.savedJob.def == MeeseeksDefOf.CM_Meeseeks_Box_Job_Kill || pawn.Drafted);

            float distanceToTarget = compMeeseeksMemory.savedJob.targetA.Cell.DistanceTo(pawn.PositionHeld);
            float maxTotalDistance = distanceToTarget * 1.5f;
            float maxDistanceToApparel = distanceToTarget;

            if (!combatJob)
            {
                maxTotalDistance *= 2.0f;
                maxDistanceToApparel *= 2.0f;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            Apparel selectedApparel = null;
            float selectedApparelScore = 0;

            neededWarmth = PawnApparelGenerator.CalculateNeededWarmth(pawn, pawn.Map.Tile, GenLocalDate.Twelfth(pawn));

            foreach (Thing apparelThing in availableApparel)
            {
                Apparel apparel = apparelThing as Apparel;
                float distanceToApparel = apparel.PositionHeld.DistanceTo(pawn.PositionHeld);
                if (distanceToApparel <= maxDistanceToApparel && (distanceToApparel + distanceToTarget) <= maxTotalDistance)
                {
                    float apparelScore = ApparelScoreRaw(pawn, apparel);
                    float distanceFactor = distanceToApparel + 1.0f;
                    apparelScore = (apparelScore * apparelScore) / (distanceFactor * distanceFactor);

                    if (DebugViewSettings.debugApparelOptimize)
                    {
                        debugSb.AppendLine(apparel.LabelCap + ": " + apparelScore.ToString("F2"));
                    }
                    if (apparelScore > selectedApparelScore)
                    {
                        selectedApparel = apparel;
                        selectedApparelScore = apparelScore;
                    }
                }
            }

            if (DebugViewSettings.debugApparelOptimize)
            {
                debugSb.AppendLine("BEST: " + selectedApparel);
                Log.Message(debugSb.ToString());
                debugSb = null;
            }

            if (selectedApparel == null)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }

            return JobMaker.MakeJob(JobDefOf.Wear, selectedApparel);
        }

        private bool CanWearThis(Pawn pawn, Thing apparel, List<SkillDef> relevantSkills)
        {
            if (apparel.IsForbidden(pawn))
                return false;
            if (apparel.IsBurning())
                return false;
            if (EquipmentUtility.IsBiocoded(apparel))
                return false;
            if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                return false;
            if (!pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger()))
                return false;

            bool armor = false;
            foreach (ThingCategoryDef thingCategoryDef in apparel.def.thingCategories)
            {
                if (thingCategoryDef.defName.ToLower().Contains("armor".ToLower()))
                {
                    armor = true;
                    break;
                }
            }

            if (pawn.Drafted)
            {
                if (!armor)
                    return false;
            }
            else
            {
                if (armor)
                    return false;

                if (!HasReleventStatModifiers(apparel, relevantSkills))
                    return false;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, apparel.def, pawn.RaceProps.body))
                {
                    return false;
                }
            }

            return true;
        }

        public static float ApparelScoreRaw(Pawn pawn, Apparel ap)
        {
            if (ap is ShieldBelt && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWeaponUsingProjectiles)
            {
                return -1000f;
            }

            float score = 0.1f + ap.def.apparel.scoreOffset;
            float armorScore = ap.GetStatValue(StatDefOf.ArmorRating_Sharp) + ap.GetStatValue(StatDefOf.ArmorRating_Blunt) + ap.GetStatValue(StatDefOf.ArmorRating_Heat);
            score += armorScore;

            if (ap.def.useHitPoints)
            {
                float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                score *= HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            score += ap.GetSpecialApparelScoreOffset();

            float warmthScore = 1f;
            if (neededWarmth == NeededWarmth.Warm)
            {
                float statValue = ap.GetStatValue(StatDefOf.Insulation_Cold);
                warmthScore *= InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValue);
            }
            score *= warmthScore;

            // Human body is close enough
            float coverageScore = ap.def.apparel.HumanBodyCoverage * 10.0f;
            score *= coverageScore;

            return score;
        }

        private static bool HasReleventStatModifiers(Thing apparel, List<SkillDef> relevantSkills)
        {
            List<StatModifier> statModifiers = apparel.def.equippedStatOffsets;
            if (relevantSkills != null && statModifiers != null)
            {
                //Logger.MessageFormat(this, "Found relevantSkills...");
                foreach (StatModifier statModifier in statModifiers)
                {
                    List<SkillNeed> skillNeedOffsets = statModifier.stat.skillNeedOffsets;
                    List<SkillNeed> skillNeedFactors = statModifier.stat.skillNeedFactors;

                    if (skillNeedOffsets != null)
                    {
                        //Logger.MessageFormat(this, "Found skillNeedOffsets...");
                        foreach (SkillNeed skillNeed in skillNeedOffsets)
                        {
                            if (relevantSkills.Contains(skillNeed.skill))
                            {
                                //Logger.MessageFormat(this, "{0} has {1}, relevant to {2}", apparel.Label, statModifier.stat.label, skillNeed.skill);
                                return true;
                            }
                        }
                    }

                    if (skillNeedFactors != null)
                    {
                        foreach (SkillNeed skillNeed in skillNeedFactors)
                        {
                            if (relevantSkills.Contains(skillNeed.skill))
                            {
                                //Logger.MessageFormat(this, "{0} has {1}, relevant to {2}", apparel.Label, statModifier.stat.label, skillNeed.skill);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
