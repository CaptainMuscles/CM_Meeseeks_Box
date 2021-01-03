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
        private bool cooldownTicksCalculated = false;
        private int cooldownTicksTotal = 600;

        public override int CooldownTicksTotal
        {
            get
            {
                if (!cooldownTicksCalculated)
                {
                    cooldownTicksCalculated = true;

                    ThingComp owningThingComp = (this.DirectOwner as ThingComp);

                    ThingWithComps theBox = owningThingComp.parent;

                    QualityCategory boxQuality = QualityCategory.Normal;
                    theBox.TryGetQuality(out boxQuality);

                    int qualityInt = (int)boxQuality;

                    float cooldownMultiplier = ((float)(qualityInt + 1)) / ((int)QualityCategory.Legendary + 1);

                    cooldownTicksTotal = (int)(cooldownTicksTotalBase * cooldownMultiplier * cooldownMultiplier);

                    //Logger.MessageFormat(this, "Cooldown ticks set to {0}", cooldownTicksTotal);
                }

                return cooldownTicksTotal;
            }
        }

        protected override bool TryCastShot()
        {
            SpawnMeeseeks();
            cooldownTicksRemaining = CooldownTicksTotal;
            return true;
        }

        private void SpawnMeeseeks()
        {
            ThingComp owningThingComp = (this.DirectOwner as ThingComp);

            ThingWithComps theBox = owningThingComp.parent;

            MeeseeksUtility.SpawnMeeseeks(this.CasterPawn, theBox, caster.Map);
        }
    }
}
