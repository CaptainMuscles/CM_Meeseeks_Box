using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_Meeseeks_Box
{
    public class MeeseeksModSettings : ModSettings
    {
        public bool autoSelectOnCreation = true;
        public bool cameraJumpOnCreation = false;
        public bool autoPauseOnCreation = false;
        public bool meeseeksSpeaks = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref autoSelectOnCreation, "autoSelectOnCreation", true);
            Scribe_Values.Look(ref cameraJumpOnCreation, "cameraJumpOnCreation", false);
            Scribe_Values.Look(ref autoPauseOnCreation, "autoPauseOnCreation", false);
            Scribe_Values.Look(ref meeseeksSpeaks, "meeseeksSpeaks", true);
        }

        public void UpdateSettings()
        {
        }
    }
}
