using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    class CompMeeseeksMemory : ThingComp
    {
        private bool givenTask = false;
        private bool startedTask = false;
        private bool taskCompleted = false;

        public SavedJob savedJob = null;
        public JobDef lastStartedJobDef = null;

        public int givenTaskTick = -1;
        public int acquiredEquipmentTick = -1;
        public int checkedForClothingTick = -1;
        public BodyPartGroupDef lastCheckedBodyPartGroup = null;

        private bool destroyed = false;

        private static int maxQueueOrderTicks = 300;

        public Voice voice = new Voice();

        private bool playedAcceptSound = false;

        private List<string> jobList = new List<string>();
        private List<string> jobResults = new List<string>();
        public List<LocalTargetInfo> jobTargets = new List<LocalTargetInfo>();

        private static List<JobDef> freeJobs = new List<JobDef>();//{ JobDefOf.Equip, JobDefOf.Goto };
        public static List<JobDef> noContinueJobs = new List<JobDef> { JobDefOf.Goto };

        public CompProperties_MeeseeksMemory Props => (CompProperties_MeeseeksMemory)props;

        public bool GivenTask => givenTask;

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

        public string GetDebugInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Jobs started: ");

            for (int i = 0; i < Math.Max(jobList.Count, jobResults.Count); ++i)
            {
                string jobName = "???";
                if (i < jobList.Count)
                    jobName = jobList[i];

                string jobResult = "???";
                if (i < jobResults.Count)
                    jobResult = jobResults[i];

                stringBuilder.AppendLine(jobName + " - " + jobResult);
            }

            return stringBuilder.ToString();
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

            //clothingPartPriority = new List<BodyPartGroupDef>();
            //clothingPartPriority.Add(BodyPartGroupDefOf.FullHead);
            //clothingPartPriority.Add(BodyPartGroupDefOf.UpperHead);
            //clothingPartPriority.Add(BodyPartGroupDefOf.Eyes);
            //clothingPartPriority.Add(BodyPartGroupDefOf.Torso);
            //clothingPartPriority.Add(BodyPartGroupDefOf.Legs);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.givenTask, "givenTask", false);
            Scribe_Values.Look<bool>(ref this.startedTask, "startedTask", false);
            Scribe_Values.Look<bool>(ref this.taskCompleted, "taskCompleted", false);
            Scribe_Values.Look<bool>(ref this.playedAcceptSound, "playedAcceptSound", false);
            
            Scribe_Values.Look<int>(ref this.givenTaskTick, "givenTaskTick", -1);
            Scribe_Values.Look<int>(ref this.acquiredEquipmentTick, "acquiredWeaponTick", -1);
            Scribe_Values.Look<int>(ref this.checkedForClothingTick, "checkedForClothingTick", -1);

            Scribe_Deep.Look(ref savedJob, "savedJob");
            Scribe_Defs.Look(ref lastStartedJobDef, "lastStartedJobDef");

            Scribe_Deep.Look(ref voice, "Voice");

            Scribe_Collections.Look(ref jobList, "jobList");
            Scribe_Collections.Look(ref jobResults, "jobResult");
            Scribe_Collections.Look(ref jobTargets, "jobTargets", LookMode.LocalTargetInfo);

            if (jobList == null)
                jobList = new List<string>();
            if (jobResults == null)
                jobResults = new List<string>();
            if (jobTargets == null)
                jobTargets = new List<LocalTargetInfo>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit && voice == null)
            {
                voice = new Voice();
            }
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

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            Logger.MessageFormat(this, "Meeseeks despawned \n{0}", this.GetDebugInfo());
        }

        public void AddJobTarget(LocalTargetInfo target, WorkGiverDef workGiverDef)
        {
            // Construction jobs need to track the cell in order to advance to the next stage when searching for jobs later
            if (target.HasThing && workGiverDef != null && WorkerDefUtility.GetCombinedDefs(workGiverDef).Contains(workGiverDef))
            {
                AddJobTarget(target.Cell);
            }
            else
            {
                AddJobTarget(target);
            }
        }

        public void AddJobTarget(LocalTargetInfo target)
        {
            if (!jobTargets.Contains(target))
                jobTargets.Add(target);
        }

        public void SortJobTargets()
        {
            jobTargets.Sort((a, b) => (int)(Meeseeks.PositionHeld.DistanceTo(a.Cell) - Meeseeks.PositionHeld.DistanceTo(b.Cell)));
        }

        public bool HasTimeToQueueNewJob()
        {
            int ticksSinceOrder = Find.TickManager.TicksGame - givenTaskTick;
            return (givenTask && ticksSinceOrder < maxQueueOrderTicks && savedJob != null && !noContinueJobs.Contains(savedJob.def));
        }

        public bool CanTakeOrders()
        {
            if (!givenTask)
                return true;

            bool canQueueNewJob = (KeyBindingDefOf.QueueOrder.IsDownEvent && HasTimeToQueueNewJob());

            return canQueueNewJob;
        }

        public bool CanTakeOrderedJob(Job job)
        {
            if (!givenTask)
                return true;

            bool canQueueNewJob = (KeyBindingDefOf.QueueOrder.IsDownEvent && HasTimeToQueueNewJob());
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
                playedAcceptSound = true;
                MeeseeksUtility.PlayAcceptTaskSound(this.parent, voice);
            }
        }

        public void StartedJob(Job job)
        {
            if (job == null)
            {
                Logger.MessageFormat(this, "Meeseeks attempting to start null job...");
                return;
            }
            string jobName = job.def.defName;
            lastStartedJobDef = job.def;

            jobList.Add(jobName);

            //Logger.MessageFormat(this, "Meeseeks has started job: {0}", jobName);

            // We don't check for givenTask here because we want further forced jobs as given by the Achtung mod to become the new current saved job
            if (job.playerForced && !freeJobs.Contains(job.def))
            {
                if (!startedTask)
                {
                    startedTask = true;
                    givenTask = true;
                    givenTaskTick = Find.TickManager.TicksGame;

                    if (!playedAcceptSound)
                    {
                        MeeseeksUtility.PlayAcceptTaskSound(this.parent, voice);
                        playedAcceptSound = true;
                    }

                    TargetIndex targetIndex = GetJobPrimaryTarget(job);

                    if (targetIndex != TargetIndex.None)
                    {
                        AddJobTarget(job.GetTarget(targetIndex), job.workGiverDef);
                    }
                    else
                    {
                        Logger.MessageFormat(this, "No target found for {0}", job.def.defName);
                    }

                    savedJob = new SavedJob(job);
                }

                //Logger.MessageFormat(this, "Meeseeks has accepted task: {0}", savedJob.def.defName);
            }
        }

        private TargetIndex GetJobPrimaryTarget(Job job)
        {
            TargetIndex result = TargetIndex.None;

            if (job.workGiverDef != null && job.workGiverDef.Worker != null)
            {
                WorkGiver_Scanner workGiverScanner = job.workGiverDef.Worker as WorkGiver_Scanner;
                if (workGiverScanner != null)
                {
                    if (this.HasSameJobOnThing(job, workGiverScanner, job.targetA))
                        return TargetIndex.A;
                    if (this.HasSameJobOnThing(job, workGiverScanner, job.targetB))
                        return TargetIndex.B;
                    if (this.HasSameJobOnThing(job, workGiverScanner, job.targetC))
                        return TargetIndex.C;

                    if (this.HasSameJobOnCell(job, workGiverScanner, job.targetA))
                        return TargetIndex.A;
                    if (this.HasSameJobOnCell(job, workGiverScanner, job.targetB))
                        return TargetIndex.B;
                    if (this.HasSameJobOnCell(job, workGiverScanner, job.targetC))
                        return TargetIndex.C;
                }
                else
                {
                    //Logger.MessageFormat(this, "Non scanner WorkGiver", job.workGiverDef.defName);
                }
            }
            else
            {
                //Logger.MessageFormat(this, "No workGiverDef found for {0}", job.def.defName);
            }

            if (result == TargetIndex.None)
            {
                // Let the hacks begin!
                // First, hauling to a construction job
                if (job.def == JobDefOf.HaulToContainer && WorkerDefUtility.constructionDefs.Contains(job.workGiverDef))
                {
                    if (job.targetC.IsValid)
                        result = TargetIndex.C;
                }

                if (result == TargetIndex.None)
                {
                    // There shold only be a few non scanner, non workGiver jobs that a Meeseeks could get, lets default to whatever targets we have if possible
                    if (HasValidTarget(job.targetA))
                        result = TargetIndex.A;
                    if (HasValidTarget(job.targetB))
                        result = TargetIndex.B;
                    if (HasValidTarget(job.targetC))
                        result = TargetIndex.C;


                    if (result != TargetIndex.None)
                        Logger.WarningFormat(this, "Had to default to any target for job {0}", job.def.defName);
                    else
                        Logger.WarningFormat(this, "Could not find any target at all for job {0}", job.def.defName);
                }
            }

            return result;
        }

        private bool HasSameJobOnThing(Job job, WorkGiver_Scanner workGiverScanner, LocalTargetInfo targetInfo)
        {
            if (HasValidTarget(targetInfo) && targetInfo.HasThing)
            {
                Job getJob = workGiverScanner.JobOnThing(Meeseeks, targetInfo.Thing, true);
                if (getJob != null && getJob.def == job.def)
                    return true;
            }

            return false;
        }

        private bool HasSameJobOnCell(Job job, WorkGiver_Scanner workGiverScanner, LocalTargetInfo targetInfo)
        {
            if (HasValidTarget(targetInfo) && !targetInfo.HasThing)
            {
                Job getJob = workGiverScanner.JobOnCell(Meeseeks, targetInfo.Cell, true);
                if (getJob != null && getJob.def == job.def)
                    return true;
            }

            return false;
        }

        private bool HasValidTarget(LocalTargetInfo targetInfo)
        {
            return (targetInfo != null && this.TargetValid(targetInfo));
        }

        private bool HasInvalidTarget(LocalTargetInfo targetInfo)
        {
            return (targetInfo != null && !this.TargetValid(targetInfo));
        }

        private bool TargetValid(LocalTargetInfo targetInfo)
        {
            if (!targetInfo.IsValid)
                return false;

            if (!targetInfo.HasThing )
                return true;

            Thing target = targetInfo.Thing;

            if (target.Destroyed)
                return false;

            //Pawn pawn = targetInfo.Pawn;
            //if (pawn != null && pawn.Dead)
            //    return false;

            return true;
        }

        public void EndCurrentJob(Job job, JobDriver jobDriver, JobCondition condition)
        {
            //Logger.MessageFormat(this, "Meeseeks has finished job: {0}, {1}", job.def.defName, condition.ToString());

            jobResults.Add(condition.ToString());

            if (givenTask)
            {
                if (job.def.defName == savedJob.def.defName)
                {
                    //switch (condition)
                    //{
                    //    case JobCondition.Incompletable:
                    //        JobIncompletable(job);
                    //        break;
                    //}
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
                            //JobSucceeded(job);
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

        public string GetSummary()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(Meeseeks.GetUniqueLoadID());

            stringBuilder.AppendLine("Jobs started: ");
            foreach (string jobName in jobList)
            {
                stringBuilder.AppendLine(jobName);
            }

            return stringBuilder.ToString();
        }
    }
}
