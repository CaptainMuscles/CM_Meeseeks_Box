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
    public class JobGiver_AcquireAppropriateWeapon : ThinkNode_JobGiver
    {
        private static StringBuilder debugSb;

        private float MinMeleeWeaponDPSThreshold
        {
            get
            {
                List<Tool> tools = MeeseeksDefOf.MeeseeksRace.tools;
                float num = 0f;
                for (int i = 0; i < tools.Count; i++)
                {
                    if (tools[i].linkedBodyPartsGroup == BodyPartGroupDefOf.LeftHand || tools[i].linkedBodyPartsGroup == BodyPartGroupDefOf.RightHand)
                    {
                        num = tools[i].power / tools[i].cooldownTime;
                        break;
                    }
                }
                return num + 2f;
            }
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AcquireAppropriateWeapon obj = (JobGiver_AcquireAppropriateWeapon)base.DeepCopy(resolve);
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.equipment == null)
            {
                return null;
            }
            if (pawn.RaceProps.Humanlike && pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            if (pawn.GetRegion() == null)
            {
                return null;
            }

            //if (pawn.Faction != Faction.OfPlayer)
            //{
            //    Log.ErrorOnce(string.Concat("Non-colonist ", pawn, " tried to acquire weapon."), 764323);
            //    return null;
            //}
            if (pawn.IsQuestLodger())
            {
                return null;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                //Logger.MessageFormat(this, "Current tick: {0}, next optimize tick: {1}", Find.TickManager.TicksGame, pawn.mindState.nextApparelOptimizeTick);
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return null;
                }
            }
            else
            {
                debugSb = new StringBuilder();
                debugSb.AppendLine(string.Concat("JobGiver_AcquireAppropriateWeapon - Scanning for ", pawn, " at ", pawn.Position));
            }

            CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();
            if (compMeeseeksMemory == null || compMeeseeksMemory.savedJob == null || compMeeseeksMemory.savedJob.targetA == null || !compMeeseeksMemory.savedJob.targetA.IsValid)
                return null;

            List<SkillDef> relevantSkills = null;
            if (compMeeseeksMemory.savedJob.workGiverDef == null)
                relevantSkills = new List<SkillDef>();
            else
                relevantSkills = compMeeseeksMemory.savedJob.workGiverDef.workType.relevantSkills;

            if (compMeeseeksMemory.savedJob.def == JobDefOf.AttackMelee)
                relevantSkills.Add(SkillDefOf.Melee);
            if (compMeeseeksMemory.savedJob.def == JobDefOf.AttackStatic)
                relevantSkills.Add(SkillDefOf.Shooting);

            bool combatJob = (relevantSkills.Contains(SkillDefOf.Shooting) || relevantSkills.Contains(SkillDefOf.Melee));

            float distanceToTarget = compMeeseeksMemory.savedJob.targetA.Cell.DistanceTo(pawn.PositionHeld);
            float maxTotalDistance = distanceToTarget * 1.5f;
            float maxDistanceToWeapon = distanceToTarget;

            if (!combatJob)
            {
                maxTotalDistance *= 2.0f;
                maxDistanceToWeapon *= 2.0f;
            }

            List<Thing> weapons = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
            if (weapons.Count == 0)
                return null;

            Thing selectedWeapon = null;
            float selectedWeaponScore = GetWeaponScore(pawn.equipment.Primary, relevantSkills);
            foreach(Thing weapon in weapons)
            {
                if (EquipmentUtility.IsBiocoded(weapon))
                    continue;

                float distanceToWeapon = weapon.PositionHeld.DistanceTo(pawn.PositionHeld);
                if (distanceToWeapon <= maxDistanceToWeapon && (distanceToWeapon + distanceToTarget) <= maxTotalDistance)
                {
                    float weaponScore = GetWeaponScore(weapon, relevantSkills);
                    float distanceFactor = distanceToWeapon + 1.0f;
                    weaponScore /= (distanceFactor * distanceFactor);
                    if (DebugViewSettings.debugApparelOptimize && weaponScore > 0.0f)
                    {
                        debugSb.AppendLine(weapon.LabelCap + ": " + weaponScore.ToString("F2"));
                    }

                    if (weaponScore > selectedWeaponScore)
                    {
                        selectedWeapon = weapon;
                        selectedWeaponScore = weaponScore;
                    }
                }
            }

            if (DebugViewSettings.debugApparelOptimize)
            {
                debugSb.AppendLine("BEST: " + selectedWeapon);
                Log.Message(debugSb.ToString());
                debugSb = null;
            }

            if (selectedWeapon != null)
            {
                return JobMaker.MakeJob(JobDefOf.Equip, selectedWeapon);
            }

            return null;
        }

        private float GetWeaponScore(Thing weapon, List<SkillDef> relevantSkills)
        {
            if (weapon == null)
            {
                return 0.0f;
            }

            if (relevantSkills.Contains(SkillDefOf.Melee))
            {
                if (weapon.def.IsMeleeWeapon)
                {
                    float averageDPS = weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
                    if (averageDPS > this.MinMeleeWeaponDPSThreshold)
                        return weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
                }
                    
                return 0.0f;
            }

            if (relevantSkills.Contains(SkillDefOf.Shooting))
            {
                if (weapon.def.IsRangedWeapon && weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown) > 0.0f && !weapon.def.Verbs.NullOrEmpty())
                {
                    float damage = 0.0f;
                    VerbProperties verb = weapon.def.Verbs.First((VerbProperties x) => x.isPrimary);
                    if (verb != null)
                    {
                        if (verb.defaultProjectile != null)
                            damage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                        if (verb.burstShotCount != 0)
                            damage *= verb.burstShotCount;
                    }

                    return damage / weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
                }
                    
                return 0.0f;
            }

            if (HasReleventStatModifiers(weapon, relevantSkills))
                return 1.0f;

            return 0.0f;
        }

        private bool HasReleventStatModifiers(Thing weapon, List<SkillDef> relevantSkills)
        {
            List<StatModifier> statModifiers = weapon.def.equippedStatOffsets;
            if (relevantSkills != null && statModifiers != null)
            {
                Logger.MessageFormat(this, "Found relevantSkills...");
                foreach (StatModifier statModifier in statModifiers)
                {
                    List<SkillNeed> skillNeedOffsets = statModifier.stat.skillNeedOffsets;
                    List<SkillNeed> skillNeedFactors = statModifier.stat.skillNeedFactors;

                    if (skillNeedOffsets != null)
                    {
                        Logger.MessageFormat(this, "Found skillNeedOffsets...");
                        foreach (SkillNeed skillNeed in skillNeedOffsets)
                        {
                            if (relevantSkills.Contains(skillNeed.skill))
                            {
                                Logger.MessageFormat(this, "{0} has {1}, relevant to {2}", weapon.Label, statModifier.stat.label, skillNeed.skill);
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
                                Logger.MessageFormat(this, "{0} has {1}, relevant to {2}", weapon.Label, statModifier.stat.label, skillNeed.skill);
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