using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_Meeseeks_Box
{
    public class MeeseeksMod : Mod
    {
        private static MeeseeksMod _instance;
        public static MeeseeksMod Instance => _instance;

        public static MeeseeksModSettings settings;

        public override string SettingsCategory()
        {
            return "Meeseeks Box";
        }

        public MeeseeksMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("CM_Meeseeks_Box");
            harmony.PatchAll();

            _instance = this;

            settings = GetSettings<MeeseeksModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingAutoSelectOnCreationLabel".Translate(), ref settings.autoSelectOnCreation, "CM_Meeseeks_Box_SettingAutoSelectOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingJumpCameraOnCreationLabel".Translate(), ref settings.cameraJumpOnCreation, "CM_Meeseeks_Box_SettingJumpCameraOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingAutoPauseOnCreationLabel".Translate(), ref settings.autoPauseOnCreation, "CM_Meeseeks_Box_SettingAutoPauseOnCreationDescription".Translate());
            listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_SettingMeeseeksSpeaksLabel".Translate(), ref settings.meeseeksSpeaks, "CM_Meeseeks_Box_SettingMeeseeksSpeaksDescription".Translate());
            listing_Standard.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            settings.UpdateSettings();
        }
    }
}
