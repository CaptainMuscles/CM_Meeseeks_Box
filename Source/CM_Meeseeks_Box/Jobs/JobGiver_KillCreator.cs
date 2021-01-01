using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CM_Meeseeks_Box
{
    public class JobGiver_KillCreator : ThinkNode_JobGiver
    {
        private int recheckInterval = 120;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_KillCreator obj = (JobGiver_KillCreator)base.DeepCopy(resolve);
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            if (pawn.GetRegion() == null)
            {
                return null;
            }

            CompMeeseeksMemory memory = pawn.GetComp<CompMeeseeksMemory>();
            if (memory == null)
                return null;

            //MentalState_MeeseeksKillCreator mentalState = pawn.MentalState as MentalState_MeeseeksKillCreator;
            //if (mentalState == null)
            //    return null;

            //Pawn creator = mentalState.target;

            Lord lord = pawn.GetLord();
            if (lord == null)
                return null;

            LordJob_MeeseeksKillCreator killCreatorLordJob = lord.LordJob as LordJob_MeeseeksKillCreator;
            if (killCreatorLordJob == null)
                return null;

            Pawn creator = killCreatorLordJob.Target;

            Job job = null;

            if (creator == null || creator.Dead || creator.Destroyed)
                job = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_EmbraceTheVoid);

            if (creator.MapHeld != pawn.MapHeld)
                job = ExitMap(pawn);

            if (job == null)
                job = TryMeleeAttackCreatorJob(pawn, creator.SpawnedParentOrMe);

            if (job == null)
                job = TryRangedAttackCreatorJob(pawn, creator.SpawnedParentOrMe);

            if (job == null)
                job = TryMeleeAttackAdjacentJob(pawn);

            if (job == null)
            {
                Thing selectedEquipment = JobDriver_AcquireEquipment.FindEquipment(pawn);

                if (selectedEquipment != null && pawn.PositionHeld.DistanceTo(selectedEquipment.PositionHeld) < pawn.PositionHeld.DistanceTo(creator.PositionHeld))
                    job = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_AcquireEquipment, selectedEquipment);
            }

            if (job == null)
                job = GoNearCreator(pawn, creator.SpawnedParentOrMe);

            return job;
        }

        private Job TryMeleeAttackCreatorJob(Pawn pawn, Thing creator)
        {
            Job newJob = null;

            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!c.InBounds(pawn.Map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(pawn.Map);

                foreach (Thing thing in thingList)
                {
                    if (thing == creator)
                    {
                        newJob = JobMaker.MakeJob(JobDefOf.AttackMelee, creator);
                        newJob.maxNumMeleeAttacks = 1;
                        newJob.killIncappedTarget = true;
                        newJob.collideWithPawns = true;
                        newJob.expiryInterval = recheckInterval;
                        newJob.checkOverrideOnExpire = true;
                        return newJob;
                    }
                }
            }

            return null;
        }

        private Job TryRangedAttackCreatorJob(Pawn pawn, Thing creator)
        {
            Job newJob = null;

            Verb verb = pawn.CurrentEffectiveVerb;

            if (pawn.equipment.Primary != null && !pawn.equipment.Primary.def.IsMeleeWeapon && pawn.Position.DistanceTo(creator.Position) < verb.verbProps.range && AttackTargetFinder.CanSee(pawn, creator))
            {
                newJob = JobMaker.MakeJob(JobDefOf.AttackStatic, creator);
                newJob.maxNumStaticAttacks = 1;
                newJob.collideWithPawns = true;
                newJob.expiryInterval = recheckInterval;
                newJob.checkOverrideOnExpire = true;
                return newJob;
            }

            return null;
        }

        private Job TryMeleeAttackAdjacentJob(Pawn pawn)
        {
            Job newJob = null;

            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!c.InBounds(pawn.Map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(pawn.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Pawn target = thingList[j] as Pawn;
                    if (target != null && !target.Downed && target.HostileTo(pawn) && GenHostility.IsActiveThreatTo(target, pawn.Faction))
                    {
                        newJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                        newJob.maxNumMeleeAttacks = 1;
                        newJob.killIncappedTarget = false;
                        newJob.collideWithPawns = true;
                        newJob.expiryInterval = recheckInterval;
                        newJob.checkOverrideOnExpire = true;
                        return newJob;
                    }
                }
            }

            return null;
        }

        private Job GoNearCreator(Pawn pawn, Thing creator)
        {
            Job newJob = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_ApproachTarget, creator);
            newJob.checkOverrideOnExpire = true;
            newJob.expiryInterval = recheckInterval;
            newJob.collideWithPawns = true;
            newJob.checkOverrideOnExpire = true;
            return newJob;
        }

        private Job GoToCreator(Pawn pawn, Thing creator)
        {
            Job newJob = JobMaker.MakeJob(JobDefOf.Goto, creator);
            newJob.checkOverrideOnExpire = true;
            newJob.expiryInterval = recheckInterval;
            newJob.collideWithPawns = true;
            newJob.checkOverrideOnExpire = true;
            return newJob;
        }

        private Job ExitMap(Pawn pawn)
        {
            bool canDig = !pawn.CanReachMapEdge();

            if (!TryFindGoodExitDest(pawn, canDig, out var dest))
            {
                return null;
            }
            if (canDig)
            {
                using (PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, dest, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings)))
                {
                    IntVec3 cellBefore;
                    Thing thing = path.FirstBlockingBuilding(out cellBefore, pawn);
                    if (thing != null)
                    {
                        Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, canMineMineables: true, canMineNonMineables: true);
                        if (job != null)
                        {
                            return job;
                        }
                    }
                }
            }
            Job job2 = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job2.exitMapOnArrival = true;
            job2.failIfCantJoinOrCreateCaravan = false;
            job2.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, LocomotionUrgency.Sprint, LocomotionUrgency.Jog);
            job2.expiryInterval = recheckInterval;
            job2.canBash = true;
            return job2;
        }

        private bool TryFindGoodExitDest(Pawn pawn, bool canDig, out IntVec3 spot)
        {
            TraverseMode mode = (canDig ? TraverseMode.PassAllDestroyableThings : TraverseMode.ByPawn);
            return RCellFinder.TryFindBestExitSpot(pawn, out spot, mode);
        }
    }
}