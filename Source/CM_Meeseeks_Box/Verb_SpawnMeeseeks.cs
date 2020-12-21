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
            if (owningThingComp != null)
            {
                ThingWithComps theBox = owningThingComp.parent;
                if (theBox != null)
                {
                    QualityCategory boxQuality = QualityCategory.Normal;
                    theBox.TryGetQuality(out boxQuality);

                    int qualityInt = (int)boxQuality;
                    skillLevel = qualityInt * 3;
                }
            }

            IntVec3 summonPosition = this.FindSpawnPosition(this.caster);
            bool jumpCamera = (this.CasterIsPawn && this.CasterPawn.IsColonistPlayerControlled);

            MeeseeksUtility.SpawnMeeseeks(summonPosition, caster.Map, skillLevel, jumpCamera);
        }

        private IntVec3 FindSpawnPosition(Thing caster)
        {
            IntVec3 spawnPosition = caster.Position;
            List<IntVec3> randomOffsets = GenAdj.AdjacentCells8WayRandomized();
            foreach (IntVec3 randomOffset in randomOffsets)
            {
                spawnPosition = caster.Position + randomOffset;
                if (spawnPosition.InBounds(caster.Map) && spawnPosition.Walkable(caster.Map))
                {
                    Building_Door building_Door = spawnPosition.GetEdifice(caster.Map) as Building_Door;
                    // TODO: Could anything other than a pawn summon a meeseeks this way? If so, change this function to use Pawn
                    if (building_Door == null)// || building_Door.CanPhysicallyPass(caster))
                    {
                        return spawnPosition;
                    }
                }
            }

            return caster.Position;
        }
    }
}
