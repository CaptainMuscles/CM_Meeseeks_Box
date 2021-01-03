using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    class CompProperties_WornGizmos : CompProperties
    {
        public bool displayGizmoWhileUndrafted = true;

        public bool displayGizmoWhileDrafted = true;

        public int coolDownTicksBase = 600;

        public KeyBindingDef hotKey;

        public CompProperties_WornGizmos()
        {
            compClass = typeof(CompWornGizmos);
        }
    }
}
