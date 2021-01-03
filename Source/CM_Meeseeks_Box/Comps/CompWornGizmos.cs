using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using System;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    class CompWornGizmos : ThingComp, IVerbOwner
    {
        public Pawn Wearer => (this.ParentHolder as Pawn_ApparelTracker)?.pawn;

        public CompProperties_WornGizmos Props => props as CompProperties_WornGizmos;

        public List<VerbProperties> VerbProperties => parent.def.Verbs;

        public List<Tool> Tools => parent.def.tools;

        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        public Thing ConstantCaster => Wearer;

        private VerbTracker verbTracker;

        public VerbTracker VerbTracker
        {
            get
            {
                if (verbTracker == null)
                {
                    verbTracker = new VerbTracker(this);
                }
                return verbTracker;
            }
        }

        public string UniqueVerbOwnerID()
        {
            return "WornGizmos_" + parent.ThingID;
        }

        public bool VerbsStillUsableBy(Pawn p)
        {
            return Wearer == p;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            base.CompTick();
            foreach (Verb allVerb in VerbTracker.AllVerbs)
            {
                Verb_Cooldown verb_Cooldown = allVerb as Verb_Cooldown;

                if (verb_Cooldown != null)
                {
                    verb_Cooldown.cooldownTicksTotalBase = this.Props.coolDownTicksBase;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // TODO: Get this and CompUseEffect_UseVerb to share verbs
            //Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
        }

        public override void CompTick()
        {
            base.CompTick();
            foreach (Verb allVerb in VerbTracker.AllVerbs)
            {
                Verb_Cooldown verb_Cooldown = allVerb as Verb_Cooldown;

                if (verb_Cooldown != null)
                {
                    verb_Cooldown.CooldownTick();
                }
            }
        }

        //public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        //{
        //    foreach (Verb verb in VerbTracker.AllVerbs)
        //    {
        //        Verb_Cooldown verbCooldown = verb as Verb_Cooldown;
        //        Action action = null;
        //        string optionLabel = verb.verbProps.label.Translate();

        //        bool cooldownReady = true;
        //        if (verbCooldown != null)
        //        {
        //            cooldownReady = (verbCooldown.cooldownTicksRemaining <= 0);

        //            if (!cooldownReady)
        //                optionLabel += " - " + "AbilityOnCooldown".Translate(verbCooldown.cooldownTicksRemaining.ToStringTicksToPeriod(allowSeconds: true, shortForm: false, canUseDecimals: false));
        //        }

        //        if (cooldownReady)
        //        {
        //            action = delegate
        //            {
        //                if (verb.verbProps.hasStandardCommand)
        //                {
        //                    verb.caster = pawn;
        //                    verb.TryStartCastOn(pawn);
        //                }
        //            };
        //        }

        //        FloatMenuOption useVerbOption = new FloatMenuOption(optionLabel, action);
        //        yield return useVerbOption;
        //    }
        //}

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            bool drafted = Wearer.Drafted;
            if ((drafted && !Props.displayGizmoWhileDrafted) || (!drafted && !Props.displayGizmoWhileUndrafted))
            {
                yield break;
            }
            ThingWithComps gear = parent;
            foreach (Verb verb in VerbTracker.AllVerbs)
            {
                if (verb.verbProps.hasStandardCommand)
                {
                    yield return CreateVerbTargetCommand(gear, verb);
                }
            }
        }

        private Command_VerbTargetWithCooldown CreateVerbTargetCommand(Thing gear, Verb verb)
        {
            verb.caster = Wearer;

            Command_VerbTargetWithCooldown command_VerbTarget = new Command_VerbTargetWithCooldown();
            command_VerbTarget.defaultDesc = gear.def.description;
            command_VerbTarget.hotKey = Props.hotKey;
            command_VerbTarget.defaultLabel = verb.verbProps.label.Translate();
            command_VerbTarget.verb = verb;
            if (verb.verbProps.defaultProjectile != null && verb.verbProps.commandIcon == null)
            {
                command_VerbTarget.icon = verb.verbProps.defaultProjectile.uiIcon;
                command_VerbTarget.iconAngle = verb.verbProps.defaultProjectile.uiIconAngle;
                command_VerbTarget.iconOffset = verb.verbProps.defaultProjectile.uiIconOffset;
                //command_Reloadable.overrideColor = verb.verbProps.defaultProjectile.graphicData.color;
            }
            else
            {
                command_VerbTarget.icon = ((verb.UIIcon != BaseContent.BadTex) ? verb.UIIcon : gear.def.uiIcon);
                command_VerbTarget.iconAngle = gear.def.uiIconAngle;
                command_VerbTarget.iconOffset = gear.def.uiIconOffset;
                command_VerbTarget.defaultIconColor = gear.DrawColor;
            }
            if (!Wearer.IsColonistPlayerControlled || !Wearer.Awake())
            {
                command_VerbTarget.Disable();
            }
            else if (verb.verbProps.violent && Wearer.WorkTagIsDisabled(WorkTags.Violent))
            {
                command_VerbTarget.Disable("IsIncapableOfViolenceLower".Translate(Wearer.LabelShort, Wearer).CapitalizeFirst() + ".");
            }
            //else if (!CanBeUsed)
            //{
            //    command_Reloadable.Disable(DisabledReason(MinAmmoNeeded(allowForcedReload: false), MaxAmmoNeeded(allowForcedReload: false)));
            //}
            return command_VerbTarget;
        }
    }
}
