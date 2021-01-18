using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CM_Meeseeks_Box
{
    public class MeeseeksModSettings : ModSettings
    {
        public bool autoSelectOnCreation = true;
        public bool cameraJumpOnCreation = false;
        public bool autoPauseOnCreation = false;
        public bool meeseeksSpeaks = true;
        public bool screenShotDebug = false;

        public bool showDebugLogMessages = false;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref autoSelectOnCreation, "autoSelectOnCreation", true);
            Scribe_Values.Look(ref cameraJumpOnCreation, "cameraJumpOnCreation", false);
            Scribe_Values.Look(ref autoPauseOnCreation, "autoPauseOnCreation", false);
            Scribe_Values.Look(ref meeseeksSpeaks, "meeseeksSpeaks", true);
            Scribe_Values.Look(ref screenShotDebug, "screenShotDebug", false);

            Scribe_Values.Look(ref showDebugLogMessages, "showDebugLogMessages", false);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingAutoSelectOnCreationLabel".Translate(), ref autoSelectOnCreation, "CM_Meeseeks_Box_SettingAutoSelectOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingJumpCameraOnCreationLabel".Translate(), ref cameraJumpOnCreation, "CM_Meeseeks_Box_SettingJumpCameraOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingAutoPauseOnCreationLabel".Translate(), ref autoPauseOnCreation, "CM_Meeseeks_Box_SettingAutoPauseOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingMeeseeksSpeaksLabel".Translate(), ref meeseeksSpeaks, "CM_Meeseeks_Box_SettingMeeseeksSpeaksDescription".Translate());

            if (Prefs.DevMode)
            {
                listing_Standard.CheckboxLabeled("screenShotDebug", ref screenShotDebug, "screenShotDebug".Translate());

                listing_Standard.CheckboxLabeled("Show debug messages".Translate(), ref showDebugLogMessages);
            }

            listing_Standard.End();
        }

        public void UpdateSettings()
        {
        }
    }
}
