﻿using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CM_Meeseeks_Box
{
    public class JobDriver_UseMeeseeksBox : JobDriver
    {
        private int useDuration = -1;

        private int makeRequestDuration = 120;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref useDuration, "useDuration", 0);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            useDuration = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompUsable>().Props.useDuration;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil toil = Toils_General.Wait(useDuration);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            if (job.targetB.IsValid)
            {
                toil.FailOnDespawnedOrNull(TargetIndex.B);
                CompTargetable compTargetable = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompTargetable>();
                if (compTargetable != null && compTargetable.Props.nonDownedPawnOnly)
                {
                    toil.FailOnDownedOrDead(TargetIndex.B);
                }
            }
            yield return toil;
            Toil use = new Toil();
            use.initAction = delegate
            {
                Pawn actor = use.actor;
                actor.CurJob.targetA.Thing.TryGetComp<CompUsable>().UsedBy(actor);
            };
            use.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return use;

            yield return Toils_General.Wait(makeRequestDuration);

            Toil request = new Toil();
            request.initAction = delegate
            {
                CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

                if (compMeeseeksMemory != null)
                {
                    Pawn newestCreated = null;
                    if (compMeeseeksMemory.CreatedMeeseeks.Count > 0)
                        newestCreated = compMeeseeksMemory.CreatedMeeseeks[compMeeseeksMemory.CreatedMeeseeks.Count - 1];

                    if (newestCreated != null)
                    {
                        CompMeeseeksMemory newCreatedMemory = newestCreated.GetComp<CompMeeseeksMemory>();

                        if (newCreatedMemory != null)
                        {
                            newCreatedMemory.CopyJobDataFrom(compMeeseeksMemory);
                            newestCreated.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }
                }

                pawn.MentalState.RecoverFromState();
            };
            request.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return request;
        }
    }
}
