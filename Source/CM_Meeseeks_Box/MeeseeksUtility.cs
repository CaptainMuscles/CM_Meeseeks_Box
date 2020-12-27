using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using Verse.Sound;

namespace CM_Meeseeks_Box
{
    public static class MeeseeksUtility
    {
        private static SoundDef[] PoofInSounds = { MeeseeksDefOf.CM_Meeseeks_Box_Poof_In };
        private static SoundDef[] PoofOutSounds = { MeeseeksDefOf.CM_Meeseeks_Box_Poof_Out };

        private static SoundDef[] AcceptTaskSounds = { MeeseeksDefOf.CM_Meeseeks_Box_Sound_OK, MeeseeksDefOf.CM_Meeseeks_Box_Sound_OK_2, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Can_Do, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Can_Do_2 };
        private static SoundDef[] FinishTaskSounds = { MeeseeksDefOf.CM_Meeseeks_Box_Sound_All_Done };
        private static SoundDef[] GreetingSounds = { MeeseeksDefOf.CM_Meeseeks_Box_Sound_Im_Mr_Meeseeks_Look_At_Me, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Look_At_Me, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Im_Mr_Meeseeks, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Im_Mr_Meeseeks_2, MeeseeksDefOf.CM_Meeseeks_Box_Sound_Im_Mr_Meeseeks_3 };

        public static TargetInfo GetTargetInfo(Thing target)
        {
            TargetInfo targetInfo = null;
            if (target != null && target.SpawnedOrAnyParentSpawned)
                targetInfo = new TargetInfo(target.PositionHeld, target.MapHeld);
            return targetInfo;
        }

        public static void PlayPoofInSound(Thing target)
        {
            PoofInSounds.RandomElement().PlayOneShot(GetTargetInfo(target));
        }

        public static void PlayPoofOutSound(Thing target)
        {
            PoofOutSounds.RandomElement().PlayOneShot(GetTargetInfo(target));
        }

        public static void PlayAcceptTaskSound(Thing target, Voice voice)
        {
            PlayVoicedSound(target, voice, AcceptTaskSounds.RandomElement());
        }

        public static void PlayFinishTaskSound(Thing target, Voice voice)
        {
            PlayVoicedSound(target, voice, FinishTaskSounds.RandomElement());
        }

        public static void PlayGreetingSound(Thing target, Voice voice)
        {
            PlayVoicedSound(target, voice, GreetingSounds.RandomElement());
        }

        public static void PlayVoicedSound(Thing target, Voice voice, SoundDef soundDef)
        {
            //Logger.MessageFormat(target, "{0} playing sound {2} with voice: {1}", target, soundDef.defName, voice);

            SoundInfo soundInfo = GetTargetInfo(target);
            soundInfo.pitchFactor = voice.pitch;
            soundInfo.volumeFactor = voice.volume;
            soundDef.PlayOneShot(soundInfo);
        }

        public static void SpawnMeeseeks(IntVec3 position, Map map, int skillLevel, bool jumpCamera)
        {
            PawnKindDef pawnKindDef = MeeseeksDefOf.MeeseeksKind;

            Pawn mrMeeseeksLookAtMe = PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfPlayer);

            // Enable all work types
            foreach (WorkTypeDef item in from w in DefDatabase<WorkTypeDef>.AllDefs
                                         where !w.alwaysStartActive && !mrMeeseeksLookAtMe.WorkTypeIsDisabled(w)
                                         select w)
            {
                // Our patch overrides the priority, but we need to make sure they are all on
                mrMeeseeksLookAtMe.workSettings.SetPriority(item, 3);
            }

            // Give minor passion in all skills
            foreach (SkillRecord skill in mrMeeseeksLookAtMe.skills.skills)
            {
                skill.Level = skillLevel;
                skill.passion = Passion.Minor;
            }

            // Max out needs
            foreach (Need need in mrMeeseeksLookAtMe.needs.AllNeeds)
            {
                if (need.def.defName != "Mood")
                    need.CurLevelPercentage = 1.0f;
            }

            GenSpawn.Spawn(mrMeeseeksLookAtMe, position, map);

            CompMeeseeksMemory compMeeseeksMemory = mrMeeseeksLookAtMe.GetComp<CompMeeseeksMemory>();
            MeeseeksUtility.PlayGreetingSound(mrMeeseeksLookAtMe, compMeeseeksMemory.voice);

            Thing smoke = ThingMaker.MakeThing(ThingDefOf.Gas_Smoke);
            GenSpawn.Spawn(smoke, position, map);
            MeeseeksUtility.PlayPoofInSound(smoke);

            //ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_PsycastSkipOuterRingExit");
            //if (moteDef != null)
            //    MoteMaker.MakeAttachedOverlay(mrMeeseeksLookAtMe, moteDef, Vector3.zero, 1.0f);

            //GenExplosion.DoExplosion(mrMeeseeksLookAtMe.PositionHeld, mrMeeseeksLookAtMe.MapHeld, 1.0f, DamageDefOf.Smoke, null, -1, -1f, MeeseeksDefOf.CM_Meeseeks_Box_Poof_In, null, null, null, ThingDefOf.Gas_Smoke, 1f);

            if (jumpCamera)
            {
                LookTargets otherSideTarget = new LookTargets(mrMeeseeksLookAtMe);
                CameraJumper.TrySelect(otherSideTarget.TryGetPrimaryTarget());
            }
        }

        public static void DespawnMeeseeks(Pawn mrMeeseeksLookAtMe)
        {
            
            Thing smoke = ThingMaker.MakeThing(ThingDefOf.Gas_Smoke);
            GenSpawn.Spawn(smoke, mrMeeseeksLookAtMe.PositionHeld, mrMeeseeksLookAtMe.MapHeld);
            MeeseeksUtility.PlayPoofOutSound(mrMeeseeksLookAtMe);

            mrMeeseeksLookAtMe.Strip();

            //ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_PsycastSkipOuterRingExit");
            //if (moteDef != null)
            //    MoteMaker.MakeAttachedOverlay(pawn, moteDef, Vector3.zero, 1.0f);

            //GenExplosion.DoExplosion(pawn.PositionHeld, pawn.MapHeld, 1.0f, DamageDefOf.Smoke, null, -1, -1f, MeeseeksDefOf.CM_Meeseeks_Box_Poof_Out, null, null, null, ThingDefOf.Gas_Smoke, 1f);
            if (mrMeeseeksLookAtMe.Corpse != null)
                mrMeeseeksLookAtMe.Corpse.Destroy();
            else
                mrMeeseeksLookAtMe.Destroy();
        }
    }
}
