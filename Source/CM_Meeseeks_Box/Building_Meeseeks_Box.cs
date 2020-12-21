using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    public class Building_Meeseeks_Box : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            // Rename
            yield return new Command_Action
            {
                //icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true),
                action = () => this.SpawnMeeseeks(),
                defaultLabel = "CM_Meeseeks_MeeseeksBox_PressButtonLabel".Translate(),
                defaultDesc = "CM_Meeseeks_MeeseeksBox_PressButtonDescription".Translate()
            };
        }

        private void SpawnMeeseeks()
        {
            PawnKindDef pawnKindDef = MeeseeksDefOf.MeeseeksKind;
            
            Pawn mrMeeseeksLookAtMe = PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfPlayer);

            // Enable all work types
            foreach (WorkTypeDef item in from w in DefDatabase<WorkTypeDef>.AllDefs
                                         where !w.alwaysStartActive && !mrMeeseeksLookAtMe.WorkTypeIsDisabled(w)
                                         select w)
            {
                mrMeeseeksLookAtMe.workSettings.SetPriority(item, 3);
            }

            // Give minor passion in all skills
            foreach(SkillRecord skill in mrMeeseeksLookAtMe.skills.skills)
            {
                skill.passion = Passion.Minor;
            }

            //newPawn.SetFaction(null);
            GenSpawn.Spawn(mrMeeseeksLookAtMe, this.Position, this.Map);

            GenExplosion.DoExplosion(mrMeeseeksLookAtMe.PositionHeld, mrMeeseeksLookAtMe.MapHeld, 1.0f, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, ThingDefOf.Gas_Smoke, 1f);
        }
    }
}
