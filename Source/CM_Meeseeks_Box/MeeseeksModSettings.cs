using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_Meeseeks_Box
{
    public class MeeseeksModSettings : ModSettings
    {
        public bool msMeeseeks = false;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref msMeeseeks, "msMeeseeks", false);
        }

        public void UpdateSettings()
        {
            if (msMeeseeks)
            {

            }
        }
    }
}
