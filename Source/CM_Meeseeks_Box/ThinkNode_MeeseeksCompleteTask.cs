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

                if (nextJob == null)
                    nextJob = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_EmbraceTheVoid);

                if (nextJob != null)
                {
                    bool reservationsMade = nextJob.TryMakePreToilReservations(pawn, false);

                    if (reservationsMade)
                        return new ThinkResult(nextJob, this, null, fromQueue: false);

                    Logger.MessageFormat(this, "Could not make reservations.");
                }

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

            LocalTargetInfo jobTarget = null;

            compMeeseeksMemory.SortJobTargets();

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
                            List<WorkGiver_Scanner> workGivers = WorkerDefUtility.GetCombinedDefs(workGiverScanner).Where(workGiverDef => workGiverDef.giverClass != null)
                                                                                                                   .Select(workGiverDef => (WorkGiver_Scanner)workGiverDef.Worker).ToList();

                            foreach(WorkGiver_Scanner scanner in workGivers)
                            {
                                nextJob = this.GetJobOnTarget(meeseeks, jobTarget, scanner);

                                if (nextJob != null)
                                {
                                    Logger.MessageFormat(this, "Job WAS found for {0}.", scanner.def.defName);
                                    break;
                                }

                                Logger.MessageFormat(this, "No {0} job found.", scanner.def.defName);
                            }

                            if (nextJob == null)
                            {
                                Logger.MessageFormat(this, "No job found for {0}.", jobTarget.ToString());
                                jobTarget = null;
                            }
                        }
                        else
                        {
                            Logger.MessageFormat(this, "No work scanner");
                        }

                        if (nextJob == null)
                            jobTarget = null;
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

                if (nextJob == null && (jobTarget == null || !jobTarget.IsValid))
                    compMeeseeksMemory.jobTargets.RemoveAt(0);
            }

            return nextJob;
        }

        private Job GetJobOnTarget(Pawn meeseeks, LocalTargetInfo targetInfo, WorkGiver_Scanner workGiverScanner)
        {
            Job job = null;

            if (targetInfo.HasThing && !targetInfo.ThingDestroyed)
                job = workGiverScanner.JobOnThing(meeseeks, targetInfo.Thing, true);

            if (job == null)
                job = workGiverScanner.JobOnCell(meeseeks, targetInfo.Cell, true);

            if (job == null)
            {
                var thingsAtCell = meeseeks.MapHeld.thingGrid.ThingsAt(targetInfo.Cell);
                foreach (Thing thing in thingsAtCell)
                {
                    Logger.MessageFormat(this, "Checking {0} for {1}.", thing.GetUniqueLoadID(), workGiverScanner.def.defName);
                    job = workGiverScanner.JobOnThing(meeseeks, thing, true);
                    if (job != null)
                        break;
                }
            }

            return job;
        }
    }
}
