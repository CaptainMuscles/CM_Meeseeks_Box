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
            Logger.MessageFormat(this, "Checking for saved job on Meeseeks.");

            CompMeeseeksMemory compMeeseeksMemory = pawn.GetComp<CompMeeseeksMemory>();

            if (compMeeseeksMemory != null)
            {
                Job savedJob = compMeeseeksMemory.GetSavedJob();
                if (savedJob != null)
                {
                    Logger.MessageFormat(this, "Meeseeks attempting to make reservation on job. {0}", savedJob.def.defName);

                    Pawn otherReserver = null;

                    if (pawn.Map != null)
                    {
                        if (savedJob.targetA != null && savedJob.targetA.IsValid)
                            otherReserver = pawn.Map.physicalInteractionReservationManager.FirstReserverOf(savedJob.targetA);
                        if (otherReserver == null && savedJob.targetB != null && savedJob.targetB.IsValid)
                            otherReserver = pawn.Map.physicalInteractionReservationManager.FirstReserverOf(savedJob.targetB);
                        if (otherReserver == null && savedJob.targetC != null && savedJob.targetC.IsValid)
                            otherReserver = pawn.Map.physicalInteractionReservationManager.FirstReserverOf(savedJob.targetC);
                    }

                    if (otherReserver == null)
                    {
                        // We're likely here because someone else was forced onto a job and interrupted us. Don't force them back off
                        bool forced = savedJob.playerForced;
                        savedJob.playerForced = false;
                        bool reservationsMade = savedJob.TryMakePreToilReservations(pawn, false);
                        savedJob.playerForced = forced;

                        if (reservationsMade)
                        {
                            Logger.MessageFormat(this, "Resuming saved job.");
                            return new ThinkResult(savedJob, this, null, fromQueue: false);
                        }
                    }
                }
            }

            return ThinkResult.NoJob;
        }
    }
}
