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
        public int cooldownTicksTotal = 600;
        public int cooldownTicksRemaining = 0;

        public virtual bool CanCast => cooldownTicksRemaining <= 0;

        public void CooldownTick()
        {
            cooldownTicksRemaining -= 1;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref cooldownTicksTotal, "cooldownTicksTotal", 0);
            Scribe_Values.Look(ref cooldownTicksRemaining, "cooldownTicksRemaining", 0);
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
            if (CasterIsPawn && CasterPawn.Downed)
            {
                reason = "CommandDisabledUnconscious".TranslateWithBackup("CommandCallRoyalAidUnconscious").Formatted(CasterPawn);
                return true;
            }

            reason = null;
            return false;
        }
    }
}
