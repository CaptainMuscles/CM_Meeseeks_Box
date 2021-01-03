using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
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

        private Job GetNextJob(Pawn meeseeks, CompMeeseeksMemory compMeeseeksMemory)
        {
            Job nextJob = null;

            SavedJob savedJob = compMeeseeksMemory.savedJob;
            if (savedJob == null)
            {
                Logger.MessageFormat(this, "No saved job...");
                return null;
            }

            if (compMeeseeksMemory.lastStartedJobDef != null && compMeeseeksMemory.lastStartedJobDef == savedJob.def)
            {
                Logger.MessageFormat(this, "Wait a tick...");
                return JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture, 2);
            }

            Logger.MessageFormat(this, "Job target count: {0}", compMeeseeksMemory.jobTargets.Count);

            SavedTargetInfo jobTarget = null;
            Map map = meeseeks.MapHeld;

            compMeeseeksMemory.SortJobTargets();

            List<SavedTargetInfo> delayedTargets = new List<SavedTargetInfo>();

            while (compMeeseeksMemory.jobTargets.Count > 0 && jobTarget == null)
            {
                jobTarget = compMeeseeksMemory.jobTargets.FirstOrDefault();

                if (jobTarget != null && jobTarget.IsValid)
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
                                    if (compMeeseeksMemory.JobStuckRepeat(nextJob))
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

                if (nextJob == null)
                    jobTarget = null;

                if (nextJob == null && (jobTarget == null || !jobTarget.IsValid))
                    compMeeseeksMemory.jobTargets.RemoveAt(0);
            }

            // Put delayed targets back on the target list
            compMeeseeksMemory.jobTargets.AddRange(delayedTargets);

            return nextJob;
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
    }
}
