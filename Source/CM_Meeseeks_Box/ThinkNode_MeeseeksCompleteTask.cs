﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace CM_Meeseeks_Box
{
    public enum JobAvailability
    {
        Invalid,
        Delayed,
        Complete,
        Available
    }

    public class ThinkNode_MeeseeksCompleteTask : ThinkNode
    {
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_QueuedJob obj = (ThinkNode_QueuedJob)base.DeepCopy(resolve);
            return obj;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            //Logger.MessageFormat(this, "Checking for saved job on Meeseeks.");

            CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

            if (compMeeseeksMemory != null && compMeeseeksMemory.GivenTask)
            {
                SavedJob savedJob = compMeeseeksMemory.savedJob;

                if (savedJob == null || CompMeeseeksMemory.noContinueJobs.Contains(savedJob.def))
                    return ThinkResult.NoJob;

                Job nextJob = GetNextJob(pawn, compMeeseeksMemory);

                if (nextJob == null && compMeeseeksMemory.jobTargets.Count == 0)
                    nextJob = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_EmbraceTheVoid);

                if (nextJob != null)
                    return new ThinkResult(nextJob, this, null, fromQueue: false);

            }

            return ThinkResult.NoJob;
        }

        private Job GetNextJob(Pawn meeseeks, CompMeeseeksMemory memory)
        {
            Job nextJob = null;

            SavedJob savedJob = memory.savedJob;
            if (savedJob == null)
            {
                Logger.MessageFormat(this, "No saved job...");
                return null;
            }

            if (memory.lastStartedJobDef != null && memory.lastStartedJobDef == savedJob.def)
            {
                Logger.MessageFormat(this, "Wait a tick...");
                return JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture, 2);
            }

            Logger.MessageFormat(this, "Job target count: {0}", memory.jobTargets.Count);

            SavedTargetInfo jobTarget = null;
            Map map = meeseeks.MapHeld;

            memory.SortJobTargets();

            List<SavedTargetInfo> delayedTargets = new List<SavedTargetInfo>();

            while (memory.jobTargets.Count > 0 && jobTarget == null)
            {
                JobAvailability jobAvailabilty = JobAvailability.Invalid;
                jobTarget = memory.jobTargets.FirstOrDefault();

                if (jobTarget.bill != null)
                {
                    nextJob = GetDoBillJob(meeseeks, memory, savedJob, jobTarget, ref jobAvailabilty);
                }
                else if (jobTarget != null && jobTarget.IsValid)
                {
                    if (savedJob.workGiverDef != null && savedJob.workGiverDef.Worker != null)
                    {
                        WorkGiver_Scanner workGiverScanner = savedJob.workGiverDef.Worker as WorkGiver_Scanner;
                        if (workGiverScanner != null)
                        {
                            List<WorkGiver_Scanner> workGivers = WorkerDefUtility.GetCombinedWorkGiverScanners(workGiverScanner);

                            foreach(WorkGiver_Scanner scanner in workGivers)
                            {
                                nextJob = this.GetJobOnTarget(meeseeks, jobTarget, scanner);

                                if (nextJob == null && jobTarget.IsConstruction)
                                {
                                    ConstructionStatus status = jobTarget.TargetConstructionStatus(map);

                                    Logger.MessageFormat(this, "Checking for blocker, construction status: {0}", status);

                                    if (status == ConstructionStatus.None)
                                    {
                                        BuildableDef buildableDef = jobTarget.BuildableDef;

                                        if (buildableDef != null)
                                        {
                                            GenConstruct.PlaceBlueprintForBuild(buildableDef, jobTarget.Cell, map, jobTarget.blueprintRotation, meeseeks.Faction, jobTarget.blueprintStuff);
                                            nextJob = this.GetJobOnTarget(meeseeks, jobTarget, scanner);
                                        }
                                    }
                                    else if (status == ConstructionStatus.Blocked)
                                    {
                                        nextJob = GetDeconstructingJob(meeseeks, jobTarget, map);
                                    }
                                }

                                if (nextJob != null)
                                {
                                    if (memory.JobStuckRepeat(nextJob))
                                    {
                                        Logger.ErrorFormat(this, "Stuck job detected and removed on {0}.", jobTarget);
                                        nextJob = null;
                                    }
                                    else
                                    {
                                        Logger.MessageFormat(this, "Job WAS found for {0}.", scanner.def.defName);
                                    }
                                    break;
                                }

                                //Logger.MessageFormat(this, "No {0} job found.", scanner.def.defName);
                            }

                            if (nextJob == null)
                            {
                                // Special case for construction jobs, resource delivery can block the job
                                if (jobTarget.IsConstruction)
                                {
                                    ConstructionStatus status = jobTarget.TargetConstructionStatus(map);
                                    if (status != ConstructionStatus.Complete)
                                    {
                                        delayedTargets.Add(jobTarget);
                                        Logger.MessageFormat(this, "Delaying construction job for {0}, construction status: {1].", jobTarget.ToString(), status);
                                    }
                                }
                                else
                                {
                                    //Logger.MessageFormat(this, "No job found for {0}.", jobTarget.ToString());
                                }

                                jobTarget = null;
                            }
                            else
                            {
                                bool reservationsMade = nextJob.TryMakePreToilReservations(meeseeks, false);
                                if (!reservationsMade)
                                {
                                    delayedTargets.Add(jobTarget);
                                    Logger.MessageFormat(this, "Delaying job for {0} because reservations could not be made.", jobTarget.ToString());

                                    nextJob = null;
                                    jobTarget = null;
                                }
                            }
                        }
                        else
                        {
                            Logger.MessageFormat(this, "No work scanner");
                        }
                    }
                    else
                    {
                        Logger.MessageFormat(this, "Missing saved job workGiverDef or Worker for savedJob: {0}", savedJob.def.defName);
                    }
                }
                else
                {
                    Logger.MessageFormat(this, "No jobTarget found.");
                }

                if (jobAvailabilty == JobAvailability.Delayed)
                    delayedTargets.Add(jobTarget);

                if (nextJob == null)
                    jobTarget = null;

                if (nextJob == null && (jobTarget == null || !jobTarget.IsValid))
                    memory.jobTargets.RemoveAt(0);
            }

            // Put delayed targets back on the target list
            memory.jobTargets.AddRange(delayedTargets);

            return nextJob;
        }

        private Job GetDoBillJob(Pawn meeseeks, CompMeeseeksMemory memory, SavedJob savedJob, SavedTargetInfo jobTarget, ref JobAvailability jobAvailabilty)
        {
            Bill bill = jobTarget.bill;
            if (bill.deleted)
            {
                jobAvailabilty = JobAvailability.Complete;
                return null;
            }

            if (bill is Bill_Medical)
            {
                return GetMedicalBillJob(meeseeks, memory, savedJob, jobTarget, ref jobAvailabilty);
            }
            else if (bill is Bill_Production)
            {
                return GetProductionBillJob(meeseeks, memory, savedJob, jobTarget, ref jobAvailabilty);
            }

            return null;
        }

        private Job GetMedicalBillJob(Pawn meeseeks, CompMeeseeksMemory memory, SavedJob savedJob, SavedTargetInfo jobTarget, ref JobAvailability jobAvailabilty)
        {
            
            Bill_Medical bill = jobTarget.bill as Bill_Medical;
            Job job = null;

            if (jobTarget != null && jobTarget.HasThing && !jobTarget.ThingDestroyed && jobTarget.Thing is Pawn && !(jobTarget.Thing as Pawn).Dead)
            {
                Pawn targetPawn = jobTarget.Thing as Pawn;
                if (targetPawn == null || targetPawn.Dead || !bill.CompletableEver)
                {
                    bill.deleted = true;
                    jobAvailabilty = JobAvailability.Complete;
                }
                else
                {
                    MeeseeksBillStorage billStorage = Current.Game.GetComponent<MeeseeksBillStorage>();
                    BillStack billStack = targetPawn.BillStack;

                    jobAvailabilty = JobAvailability.Delayed;

                    if (targetPawn.UsableForBillsAfterFueling() && meeseeks.CanReserve(targetPawn, 1, -1, null, true))
                    {
                        List<ThingCount> chosenIngredients = new List<ThingCount>();
                        // Screw you I need this function
                        bool result = (bool)typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredients", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { bill, meeseeks, targetPawn, chosenIngredients });
                            
                        if (result)
                        {
                            Job haulOffJob;
                            job = WorkGiver_DoBill.TryStartNewDoBillJob(meeseeks, bill, targetPawn, chosenIngredients, out haulOffJob);
                        }
                    }
                }
            }
            else
            {
                bill.deleted = true;
                jobAvailabilty = JobAvailability.Complete;
            }

            return job;
        }

        private Job GetProductionBillJob(Pawn meeseeks, CompMeeseeksMemory memory, SavedJob savedJob, SavedTargetInfo jobTarget, ref JobAvailability jobAvailabilty)
        {
            Bill_Production bill = jobTarget.bill as Bill_Production;
            Job job = null;

            if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount && bill.repeatCount < 1)
            {
                bill.deleted = true;
                jobAvailabilty = JobAvailability.Complete;
                return null;
            }
            else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                int productCount = bill.recipe.WorkerCounter.CountProducts(bill);
                if (productCount >= bill.targetCount)
                {
                    bill.deleted = true;
                    jobAvailabilty = JobAvailability.Complete;
                    return null;
                }
            }

            bool workStationValid = (jobTarget.HasThing && !jobTarget.ThingDestroyed &&
               meeseeks.CanReserve(jobTarget.Thing, 1, -1, null, false) &&
               ((jobTarget.Thing as IBillGiver).CurrentlyUsableForBills() || (jobTarget.Thing as IBillGiver).UsableForBillsAfterFueling()));

            if (!workStationValid)
            {
                List<Building> buildings = meeseeks.MapHeld.listerBuildings.allBuildingsColonist.Where(building => building is IBillGiver && 
                                                                                                       savedJob.workGiverDef.fixedBillGiverDefs.Contains(building.def) &&
                                                                                                       meeseeks.CanReserve(building, 1, -1, null, false) &&
                                                                                                       ((building as IBillGiver).CurrentlyUsableForBills() || (building as IBillGiver).UsableForBillsAfterFueling())).ToList();

                if (buildings.Count > 0)
                {
                    buildings.Sort((a, b) => (int)(meeseeks.PositionHeld.DistanceTo(a.Position) - meeseeks.PositionHeld.DistanceTo(b.Position)));
                    jobTarget.target = buildings[0];
                    workStationValid = true;
                }
            }

            if (workStationValid)
            {
                IBillGiver billGiver = jobTarget.Thing as IBillGiver;

                List<ThingCount> chosenIngredients = new List<ThingCount>();
                // Screw you I need this function
                bool result = (bool)typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredients", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { bill, meeseeks, jobTarget.Thing, chosenIngredients });

                if (result)
                {
                    Job haulOffJob;
                    job = WorkGiver_DoBill.TryStartNewDoBillJob(meeseeks, bill, billGiver, chosenIngredients, out haulOffJob);
                }
            }
            else
            {
                jobAvailabilty = JobAvailability.Delayed;
            }

            return job;
        }

        private Job GetJobOnTarget(Pawn meeseeks, SavedTargetInfo targetInfo, WorkGiver_Scanner workGiverScanner)
        {
            Job job = null;

            try
            {
                DesignatorUtility.ForceAllDesignationsOnCell(targetInfo.Cell, meeseeks.MapHeld);

                if (targetInfo.HasThing && !targetInfo.ThingDestroyed)
                {
                    // Special case for uninstall, the workgiver doesn't check to see if its already uninstalled
                    if (workGiverScanner as WorkGiver_Uninstall != null && !targetInfo.Thing.Spawned)
                    {
                        Logger.MessageFormat(this, "Target is inside {0}", targetInfo.Thing.ParentHolder);
                        DesignatorUtility.RestoreDesignationsOnCell(targetInfo.Cell, meeseeks.MapHeld);
                        return null;
                    }

                    if (workGiverScanner.HasJobOnThing(meeseeks, targetInfo.Thing, true))
                        job = workGiverScanner.JobOnThing(meeseeks, targetInfo.Thing, true);
                }

                if (job == null && workGiverScanner.HasJobOnCell(meeseeks, targetInfo.Cell, true))
                    job = workGiverScanner.JobOnCell(meeseeks, targetInfo.Cell, true);

                if (job == null)
                {
                    var thingsAtCell = meeseeks.MapHeld.thingGrid.ThingsAt(targetInfo.Cell);
                    foreach (Thing thing in thingsAtCell)
                    {
                        //Logger.MessageFormat(this, "Checking {0} for {1}.", thing.GetUniqueLoadID(), workGiverScanner.def.defName);
                        if (workGiverScanner.HasJobOnThing(meeseeks, thing, true))
                            job = workGiverScanner.JobOnThing(meeseeks, thing, true);
                        if (job != null)
                            break;
                    }
                }
            }
            finally
            {
                DesignatorUtility.RestoreDesignationsOnCell(targetInfo.Cell, meeseeks.MapHeld);
            }

            return job;
        }

        private Job GetDeconstructingJob(Pawn meeseeks, SavedTargetInfo jobTarget, Map map)
        {
            BuildableDef buildableDef = jobTarget.BuildableDef;
            if (buildableDef == null)
                return null;

            CellRect cellRect = GenAdj.OccupiedRect(jobTarget.Cell, jobTarget.blueprintRotation, buildableDef.Size);
            foreach (IntVec3 cell in cellRect)
            {
                foreach(Thing thing in cell.GetThingList(map))
                {
                    if (!GenConstruct.CanPlaceBlueprintOver(buildableDef, thing.def))
                        return JobMaker.MakeJob(JobDefOf.Deconstruct, thing);
                }
            }

            return null;
        }

        private bool ResolveBill(Pawn meeseeks, SavedTargetInfo jobTarget, SavedJob savedJob)
        {
            bool delay = false;
            MeeseeksBillStorage billStorage = Current.Game.GetComponent<MeeseeksBillStorage>();
            Bill bill = jobTarget.bill;
            Bill originalBill = billStorage.GetBillOriginal(bill);
            WorkGiver_DoBill workGiverDoBill = (savedJob.workGiverDef != null) ? savedJob.workGiverDef.Worker as WorkGiver_DoBill : null;

            bool isBillComplete = bill.deleted;
            bool originalDoesNotExist = originalBill.DeletedOrDereferenced;

            if (isBillComplete)
            {
                jobTarget.target = null;
            }
            else if (originalDoesNotExist)
            { 
                Bill_Medical billMedical = bill as Bill_Medical;
                Bill_Production billProduction = bill as Bill_Production;

                if (billMedical != null)
                {
                    if (jobTarget != null && jobTarget.HasThing && !jobTarget.ThingDestroyed)
                    {
                        Pawn targetPawn = jobTarget.target.Thing as Pawn;
                        if (targetPawn == null || targetPawn.Dead)
                        {
                            jobTarget.target = null;
                            bill.deleted = true;
                        }
                    }
                    else
                    {
                        bill.deleted = true;
                        jobTarget.target = null;
                    }
                }
                else if (billProduction != null)
                {
                    if (jobTarget == null || !jobTarget.HasThing || jobTarget.ThingDestroyed)
                    {
                        List<Building> buildings = meeseeks.MapHeld.listerBuildings.allBuildingsColonist;

                        buildings.Sort((a, b) => (int)(meeseeks.PositionHeld.DistanceTo(a.Position) - meeseeks.PositionHeld.DistanceTo(b.Position)));

                        delay = true;

                        foreach (Building building in buildings)
                        {
                            IBillGiver billGiver = building as IBillGiver;
                            if (billGiver != null && savedJob.workGiverDef.fixedBillGiverDefs.Contains(building.def) && meeseeks.CanReserve(building, 1, -1, null, true) && (billGiver.CurrentlyUsableForBills() || billGiver.UsableForBillsAfterFueling()))
                            {
                                jobTarget.target = building;
                                delay = false;
                                break;
                            }
                        }
                    }
                }
            }

            return delay;
        }
    }
}
