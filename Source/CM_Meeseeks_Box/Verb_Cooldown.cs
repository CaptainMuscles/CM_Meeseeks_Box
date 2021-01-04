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
    public class Verb_Cooldown : Verb
    {
        // Unfortunately, VerbTick cannot be overridden, so these will need to be altered by an outside ticking object
        public int cooldownTicksTotalBase = 600;
        //public int cooldownTicksRemaining = 0;

        private CompUseEffect_UseVerb owner = null;

        // This is dumb but I need to rework this whole thing after finding out the saved cooldown was being recognized on load
        private CompUseEffect_UseVerb Owner
        {
            get
            {
                if (owner == null)
                {
                    owner = (DirectOwner as ThingComp).parent.GetComp<CompUseEffect_UseVerb>();
                }

                return owner;
            }
        }

        public int cooldownTicksRemaining
        {
            get
            {
                return Owner.cooldownTicksRemaining;
            }
            set
            {
                Owner.cooldownTicksRemaining = value;
            }
        }

        public virtual int CooldownTicksTotal => cooldownTicksTotalBase;

        public virtual bool CanCast => cooldownTicksRemaining <= 0;

        public void CooldownTick()
        {
            Owner.cooldownTicksRemaining -= 1;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref cooldownTicksTotalBase, "cooldownTicksTotalBase", 0);
            
        }

        protected override bool TryCastShot()
        {
            return true;
        }

        public virtual bool GizmoDisabled(out string reason)
        {
            if (!CanCast)
            {
                reason = "AbilityOnCooldown".Translate(cooldownTicksRemaining.ToStringTicksToPeriod(allowSeconds: true, shortForm: false, canUseDecimals: false));
                return true;
            }
            //if (!CanQueueCast)
            //{
            //    reason = "AbilityAlreadyQueued".Translate();
            //    return true;
            //}
            if (CasterIsPawn && !CasterPawn.Awake())
            {
                reason = "CommandDisabledUnconscious".TranslateWithBackup("CommandCallRoyalAidUnconscious").Formatted(CasterPawn);
                return true;
            }

            reason = null;
            return false;
        }
    }
}
