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

        //public override void DoSettingsWindowContents(Rect inRect)
        //{
        //    base.DoSettingsWindowContents(inRect);
        //    Listing_Standard listing_Standard = new Listing_Standard();
        //    listing_Standard.Begin(inRect);
        //    listing_Standard.CheckboxLabeled("CM_Meeseeks_Box_MsMeeseeksSettingLabel", ref settings.msMeeseeks, "CM_Meeseeks_Box_MsMeeseeksSettingTooltip");
        //    listing_Standard.End();
        //}

        //public override void WriteSettings()
        //{
        //    base.WriteSettings();
        //    settings.UpdateSettings();
        //}
    }
}
