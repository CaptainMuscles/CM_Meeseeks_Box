using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    class CompMeeseeksMemory : ThingComp
    {
        private bool givenTask = false;
        private bool taskCompleted = false;

        public SavedJob savedJob = null;
        public JobDef lastStartedJobDef = null;

        public int givenTaskTick = -1;

        private bool destroyed = false;

        private static int maxQueueOrderTicks = 300;

        private static List<JobDef> freeJobs = new List<JobDef> { JobDefOf.Equip, JobDefOf.Goto };
        
        public CompProperties_MeeseeksMemory Props => (CompProperties_MeeseeksMemory)props;

        private Pawn meeseeks = null;
        public Pawn Meeseeks
        {
            get
            {
                if (meeseeks == null)
                    meeseeks = this.parent as Pawn;
                return meeseeks;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (Meeseeks != null)
            {
                bool hasPainHediff = meeseeks.health.hediffSet.HasHediff(MeeseeksDefOf.CM_Meeseeks_Box_Hediff_Existence_Is_Pain);
                if (!hasPainHediff)
                {
                    Hediff painHediff = HediffMaker.MakeHediff(MeeseeksDefOf.CM_Meeseeks_Box_Hediff_Existence_Is_Pain, meeseeks);
                    meeseeks.health.AddHediff(painHediff);
                }
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.givenTask, "givenTask", false);
            Scribe_Values.Look<bool>(ref this.taskCompleted, "taskCompleted", false);
            Scribe_Values.Look<int>(ref this.givenTaskTick, "givenTaskTick", -1);

            Scribe_Deep.Look(ref savedJob, "savedJob");
            Scribe_Defs.Look(ref lastStartedJobDef, "lastStartedJobDef");
        }

        public override void CompTick()
        {
            base.CompTick();

            if (Meeseeks.Downed)
            {
                destroyed = true;
                Logger.MessageFormat(this, "Meeseeks downed. Vanishing.");
                MeeseeksUtility.DespawnMeeseeks(Meeseeks);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (Meeseeks.Dead && !destroyed)
            {
                destroyed = true;
                Logger.MessageFormat(this, "Meeseeks dead. Vanishing.");
                MeeseeksUtility.DespawnMeeseeks(Meeseeks);
            }
        }

        public bool GivenTask()
        {
            return givenTask;
        }

        public bool CanTakeOrders()
        {
            if (!givenTask)
                return true;

            int ticksSinceOrder = Find.TickManager.TicksGame - givenTaskTick;
            bool canQueueNewJob = (KeyBindingDefOf.QueueOrder.IsDownEvent && ticksSinceOrder < maxQueueOrderTicks);

            return canQueueNewJob;
        }

        public bool CanTakeOrderedJob(Job job)
        {
            if (!givenTask)
                return true;

            int ticksSinceOrder = Find.TickManager.TicksGame - givenTaskTick;
            bool canQueueNewJob = (KeyBindingDefOf.QueueOrder.IsDownEvent && ticksSinceOrder < maxQueueOrderTicks);
            bool jobIsSame = (job.def == savedJob.def);

            return (canQueueNewJob && jobIsSame);
        }


        public void PreTryTakeOrderedJob(Job job)
        {
            // This allows Mr Meeseeks to know what clothes he could wear if he does take the job
            if (!givenTask)
            {
                Meeseeks.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame - 1;
                savedJob = new SavedJob(job);
            }
        }

        public void PostTryTakeOrderedJob(bool success, Job job)
        {
            // If he didn't take the job and hasn't been officially given one, clear out the saved job
            if (!success && !givenTask)
            {
                savedJob = null;
            }

            if (success && !givenTask && job.playerForced && !freeJobs.Contains(job.def))
            {
                givenTask = true;
                givenTaskTick = Find.TickManager.TicksGame;
                MeeseeksUtility.PlayAcceptTaskSound(this.parent);
            }
        }

        public void StartedJob(Job job)
        {
            string jobName = job.def.defName;
            lastStartedJobDef = job.def;

            //Logger.MessageFormat(this, "Meeseeks has started job: {0}", jobName);

            // We don't check for givenTask here because we want further forced jobs as given by the Achtung mod to become the new current saved job
            if (job.playerForced && !freeJobs.Contains(job.def))
            {
                givenTask = true;

                savedJob = new SavedJob(job);

                Logger.MessageFormat(this, "Meeseeks has accepted task: {0}", savedJob.def.defName);
            }
        }

        public void EndCurrentJob(Job job, JobDriver jobDriver, JobCondition condition)
        {
            Logger.MessageFormat(this, "Meeseeks has finished job: {0}, {1}", job.def.defName, condition.ToString());

            if (givenTask)
            {
                if (job.def.defName == savedJob.def.defName)
                {
                    switch (condition)
                    {
                        case JobCondition.Errored:
                            JobErrored(job);
                            break;
                        case JobCondition.Succeeded:
                            JobSucceeded(job);
                            break;
                        case JobCondition.InterruptForced:
                            JobInterrupted(job);
                            break;
                        case JobCondition.Incompletable:
                            JobIncompletable(job);
                            break;
                    }
                }
            }
        }

        private void JobErrored(Job job)
        {
            if (Meeseeks != null)
            {
                if (!taskCompleted)
                {
                    taskCompleted = true;
                    Logger.WarningFormat(this, "Meeseeks job error : {0}, marking as complete.", job.def.defName);
                }
            }
        }

        private void JobSucceeded(Job job)
        {
            if (Meeseeks != null)
            {
                if (!taskCompleted)
                {
                    taskCompleted = true;
                    Logger.MessageFormat(this, "Meeseeks succeeded at: {0}", job.def.defName);
                }
            }
        }

        private void JobInterrupted(Job job)
        {
            if (Meeseeks != null)
            {
                if (taskCompleted)
                {
                    // This job might have been marked completed in an earlier iteration (previous item in a bill or a previous part of a forced job) cancel that if interrupted.
                    taskCompleted = false;
                    Logger.MessageFormat(this, "Meeseeks interrupted at: {0}", job.def.defName);
                }
            }
        }

        private void JobIncompletable(Job job)
        {
            if (Meeseeks != null)
            {
                if (job.targetA != null)
                {
                    // If the primary target was a pawn
                    Pawn targetPawn = job.targetA.Pawn;
                    if (targetPawn != null)
                    {
                        // If its dead, we will consider ourselves successful
                        if (targetPawn.Dead)
                        {
                            JobSucceeded(job);
                        }
                        // If it is not on this map, then lets leave the map
                        else if (targetPawn.MapHeld != Meeseeks.MapHeld)
                        {
                            IntVec3 exitSpot;
                            if (RCellFinder.TryFindBestExitSpot(Meeseeks, out exitSpot, TraverseMode.ByPawn))
                            {
                                Job leaveMapJob = JobMaker.MakeJob(JobDefOf.Goto, exitSpot);
                                leaveMapJob.exitMapOnArrival = true;
                                leaveMapJob.failIfCantJoinOrCreateCaravan = false;
                                leaveMapJob.locomotionUrgency = PawnUtility.ResolveLocomotion(Meeseeks, LocomotionUrgency.Walk, LocomotionUrgency.Jog);
                                leaveMapJob.expiryInterval = 99999;
                                leaveMapJob.canBash = true;

                                Meeseeks.jobs.jobQueue.EnqueueFirst(leaveMapJob);
                            }
                        }
                    }
                }
            }
        }

        public Job GetSavedJob()
        {
            //if (savedJob != null)
            //    return savedJob;
            //else
            //    return null;

            if (savedJob != null && (HasInvalidTarget(savedJob.targetA) || HasInvalidTarget(savedJob.targetB) || HasInvalidTarget(savedJob.targetC)))
                taskCompleted = true;

            if (taskCompleted)
                return JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_EmbraceTheVoid);

            if (savedJob == null)
                return null;

            if (lastStartedJobDef != null && lastStartedJobDef == savedJob.def)
                return JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture, 2);

            return savedJob.MakeJob();
        }

        private bool HasInvalidTarget(LocalTargetInfo targetInfo)
        {
            return (targetInfo != null && !this.TargetValid(targetInfo));
        }

        private bool TargetValid(LocalTargetInfo targetInfo)
        {
            if (!targetInfo.IsValid)
                return false;

            if (!targetInfo.HasThing)
                return true;

            Thing target = targetInfo.Thing;

            if (target.Destroyed)
                return false;

            Pawn pawn = targetInfo.Pawn;
            if (pawn != null && pawn.Dead)
                return false;

            return true;
        }
    }
}
