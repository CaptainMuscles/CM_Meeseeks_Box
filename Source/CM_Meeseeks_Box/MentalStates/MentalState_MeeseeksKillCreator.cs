using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    public class MentalState_MeeseeksKillCreator : MentalState
    {
        public Pawn target;

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }

        public override void PreStart()
        {
            base.PreStart();

            CompMeeseeksMemory memory = pawn.GetComp<CompMeeseeksMemory>();

            if (memory == null)
            {
                Logger.ErrorFormat(this, "Meeseeks memory not found when starting mental state.");
                return;
            }

            Pawn creator = memory.Creator;
            CompMeeseeksMemory creatorMemory = creator.GetComp<CompMeeseeksMemory>();

            // If our creator is a Meeseeks, try his creator and so on until we get a target
            while (creator != null && creatorMemory != null)
            {
                creator = creatorMemory.Creator;
                if (creator == null)
                    creatorMemory = null;
                else
                    creatorMemory = creator.GetComp<CompMeeseeksMemory>();
            }

            Job newJob = JobMaker.MakeJob(MeeseeksDefOf.CM_Meeseeks_Box_Job_Kill, creator);
            newJob.workGiverDef = MeeseeksDefOf.CM_Meeseeks_Box_WorkGiver_Kill;
            memory.ForceNewJob(newJob, creator);

            target = creator;
        }

        public override void PostStart(string reason)
        {
            base.PostStart(reason);

            Logger.MessageFormat(this, "Meeseeks attempting to incite other Meeseeks to join in mental state.");
            CompMeeseeksMemory memory = pawn.GetComp<CompMeeseeksMemory>();
            Pawn creator = memory.Creator;
            CompMeeseeksMemory creatorMemory = creator.GetComp<CompMeeseeksMemory>();

            // If our direct creator is a Meeseeks, well then lets rope him into this
            if (creatorMemory != null && creator.Spawned && creator.Map == pawn.Map && creator.MentalStateDef != MeeseeksDefOf.CM_Meeseeks_Box_MentalState_MeeseeksKillCreator)
            {
                creator.mindState.mentalStateHandler.TryStartMentalState(MeeseeksDefOf.CM_Meeseeks_Box_MentalState_MeeseeksKillCreator);
            }

            // Now rope our direct children into this
            foreach (Pawn createdMeeseeks in memory.CreatedMeeseeks)
            {
                CompMeeseeksMemory createdMemory = createdMeeseeks.GetComp<CompMeeseeksMemory>();
                if (createdMemory != null && createdMeeseeks.Spawned && createdMeeseeks.Map == pawn.Map && createdMeeseeks.MentalStateDef != MeeseeksDefOf.CM_Meeseeks_Box_MentalState_MeeseeksKillCreator)
                {
                    createdMeeseeks.mindState.mentalStateHandler.TryStartMentalState(MeeseeksDefOf.CM_Meeseeks_Box_MentalState_MeeseeksKillCreator);
                }
            }
        }

        public override string GetBeginLetterText()
        {
            if (target == null)
            {
                Log.Error("No target. This should have been checked in this mental state's worker.");
                return "";
            }

            return def.beginLetter.Formatted(pawn.NameShortColored, target.NameShortColored, pawn.Named("PAWN"), target.Named("TARGET")).AdjustedFor(pawn).Resolve().CapitalizeFirst();
        }
    }
}