using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CM_Meeseeks_Box
{
    public class JobDriver_EmbraceTheVoid : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            MeeseeksUtility.PlayFinishTaskSound(pawn);

            int waitTime = Rand.RangeInclusive(60, 180);

            Toil wait = Toils_General.Wait(waitTime, 0);
            LocalTargetInfo faceCamera = new LocalTargetInfo(pawn.Position + IntVec3.South);
            pawn.rotationTracker.FaceTarget(faceCamera);
            yield return wait;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Logger.MessageFormat(this, "Meeseeks task completed. Vanishing.");

                    MeeseeksUtility.DespawnMeeseeks(pawn);
                }
            };
        }
    }
}
