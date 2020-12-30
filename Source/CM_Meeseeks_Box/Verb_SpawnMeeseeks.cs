using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CM_Meeseeks_Box
{
    public class Verb_SpawnMeeseeks : Verb_Cooldown
    {
        protected override bool TryCastShot()
        {
            SpawnMeeseeks();
            cooldownTicksRemaining = cooldownTicksTotal;
            return true;
        }

        private void SpawnMeeseeks()
        {
            int skillLevel = 5;

            ThingComp owningThingComp = (this.DirectOwner as ThingComp);

            ThingWithComps theBox = owningThingComp.parent;

            QualityCategory boxQuality = QualityCategory.Normal;
            theBox.TryGetQuality(out boxQuality);

            int qualityInt = (int)boxQuality;
            skillLevel = qualityInt * 3;

            MeeseeksUtility.SpawnMeeseeks(this.CasterPawn, theBox.SpawnedParentOrMe, caster.Map, skillLevel);
        }
    }
}
