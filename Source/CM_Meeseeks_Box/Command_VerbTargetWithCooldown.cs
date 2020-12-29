using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace CM_Meeseeks_Box
{
    [StaticConstructorOnStartup]
    public class Command_VerbTargetWithCooldown : Command_VerbTarget
    {
        Verb_Cooldown verbCooldown => verb as Verb_Cooldown;

        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color32(9, 203, 4, 64));

        protected virtual void DisabledCheck()
        {
            disabled = verbCooldown.GizmoDisabled(out var reason);
            if (disabled)
            {
                DisableWithReason(reason.CapitalizeFirst());
            }
        }

        protected void DisableWithReason(string reason)
        {
            disabledReason = reason;
            disabled = true;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth);
            if (verbCooldown.cooldownTicksRemaining > 0)
            {
                float num = Mathf.InverseLerp(verbCooldown.cooldownTicksTotal, 0f, verbCooldown.cooldownTicksRemaining);
                Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, doBorder: false);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, num.ToStringPercent("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (result.State == GizmoState.Interacted && verbCooldown.CanCast)
            {
                return result;
            }
            return new GizmoResult(result.State);
        }

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, bool shrunk = false)
        {
            DisabledCheck();
            return base.GizmoOnGUIInt(butRect, shrunk);
        }
    }
}
