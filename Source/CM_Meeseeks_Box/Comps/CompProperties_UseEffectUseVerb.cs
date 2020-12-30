using System;

using RimWorld;
using Verse;

namespace CM_Meeseeks_Box
{
    public class CompProperties_UseEffectUseVerb : CompProperties_UseEffect
    {
        public Type verbClass = typeof(Verb);

        public int coolDownTicks = 600;

        public CompProperties_UseEffectUseVerb()
        {
            compClass = typeof(CompUseEffect_UseVerb);
        }
    }
}