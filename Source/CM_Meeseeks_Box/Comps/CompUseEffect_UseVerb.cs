using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CM_Meeseeks_Box
{
    public class CompUseEffect_UseVerb : CompUseEffect, IVerbOwner
    {
        public CompProperties_UseEffectUseVerb Props => props as CompProperties_UseEffectUseVerb;

        public List<VerbProperties> VerbProperties => parent.def.Verbs;

        public List<Tool> Tools => parent.def.tools;

        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        public Thing ConstantCaster => null;

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
            return "UseEffect_UseVerb_" + parent.ThingID;
        }

        public bool VerbsStillUsableBy(Pawn p)
        {
            return true;
        }

        public bool OffCooldown()
        {
            foreach (Verb verb in VerbTracker.AllVerbs)
            {
                if (verb.GetType() == Props.verbClass && verb.verbProps.hasStandardCommand)
                {
                    Verb_Cooldown verbCooldown = verb as Verb_Cooldown;

                    // Note that this has not been tested with regular verbs, only Verb_Cooldown
                    if (verbCooldown != null)
                    {
                        return verbCooldown.CanCast;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return true;
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
                    verb_Cooldown.cooldownTicksTotal = this.Props.coolDownTicks;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
        }

        public override void CompTick()
        {
            base.CompTick();
            foreach (Verb verb in VerbTracker.AllVerbs)
            {
                Verb_Cooldown verbCooldown = verb as Verb_Cooldown;

                if (verbCooldown != null)
                {
                    verbCooldown.CooldownTick();
                }
            }
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            foreach (Verb verb in VerbTracker.AllVerbs)
            {
                if (verb.GetType() == Props.verbClass && verb.verbProps.hasStandardCommand)
                {
                    Verb_Cooldown verbCooldown = verb as Verb_Cooldown;

                    // Note that this has not been tested with regular verbs, only Verb_Cooldown
                    if (verbCooldown != null)
                    {
                        if (verbCooldown.CanCast)
                        {
                            verbCooldown.caster = usedBy;
                            verbCooldown.TryStartCastOn(usedBy);
                        }
                    }
                    else
                    {
                        verb.caster = usedBy;
                        verb.TryStartCastOn(usedBy);
                    }

                    break;
                }
            }
        }

        public override bool CanBeUsedBy(Pawn p, out string failReason)
        {
            foreach (Verb verb in VerbTracker.AllVerbs)
            {
                if (verb.GetType() == Props.verbClass && verb.verbProps.hasStandardCommand)
                {
                    Verb_Cooldown verbCooldown = verb as Verb_Cooldown;

                    // Note that this has not been tested with regular verbs, only Verb_Cooldown
                    if (verbCooldown != null)
                    {
                        if (!verbCooldown.CanCast)
                        {
                            failReason = "AbilityOnCooldown".Translate(verbCooldown.cooldownTicksRemaining.ToStringTicksToPeriod(allowSeconds: true, shortForm: false, canUseDecimals: false));
                            return false;
                        }
                    }
                }
            }

            failReason = null;
            return true;
        }
    }
}