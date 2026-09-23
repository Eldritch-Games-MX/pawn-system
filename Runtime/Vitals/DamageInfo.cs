using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// One immutable damage event on its way to a <see cref="Pawn"/>: how much, of what kind,
    /// from whom, and where it landed.
    /// </summary>
    /// <remarks>
    /// Passed by value and never mutated. Each <see cref="IDamageModifier"/> in the pipeline
    /// receives the current value and returns a new one, so the original is always available for
    /// comparison and nothing can be changed behind a modifier's back.
    /// <code>
    /// var damage = new DamageInfo(25f, fireType, instigator: attacker, source: weapon,
    ///                             hitPoint: hit.point, hitNormal: hit.normal);
    /// float applied = target.ApplyDamage(damage);
    /// </code>
    /// Every field beyond <see cref="Amount"/> is optional. Environmental damage has no
    /// <see cref="Instigator"/>; a scripted event has neither instigator nor hit location.
    /// </remarks>
    public readonly struct DamageInfo
    {
        /// <summary>How much vital value this event removes, before modifiers. Never negative — healing is <see cref="Pawn.Heal"/>.</summary>
        public float Amount { get; }

        /// <summary>The kind of damage, or <c>null</c> when unspecified.</summary>
        public DamageTypeDefinition Type { get; }

        /// <summary>The pawn responsible, or <c>null</c> for environmental and scripted damage.</summary>
        public Pawn Instigator { get; }

        /// <summary>The weapon, ability asset or trap that produced the damage, or <c>null</c>.</summary>
        public UnityEngine.Object Source { get; }

        /// <summary>
        /// Which hit zone was struck, as a dotted tag such as <c>"Body.Head"</c>. Set by
        /// <see cref="DamageReceiver"/>; default when the damage has no location.
        /// </summary>
        public PawnTag BodyPart { get; }

        /// <summary>World-space point of impact, or <see cref="Vector3.zero"/> when unknown.</summary>
        public Vector3 HitPoint { get; }

        /// <summary>World-space surface normal at the impact, or <see cref="Vector3.zero"/> when unknown.</summary>
        public Vector3 HitNormal { get; }

        /// <summary>Creates a damage event. Only <paramref name="amount"/> is required.</summary>
        /// <param name="amount">How much vital value to remove, before modifiers.</param>
        /// <param name="type">The kind of damage, or <c>null</c> when unspecified.</param>
        /// <param name="instigator">The pawn responsible, or <c>null</c>.</param>
        /// <param name="source">The weapon, ability asset or trap responsible, or <c>null</c>.</param>
        /// <param name="bodyPart">The hit zone struck, or default when the damage has no location.</param>
        /// <param name="hitPoint">World-space point of impact.</param>
        /// <param name="hitNormal">World-space surface normal at the impact.</param>
        public DamageInfo(
            float amount,
            DamageTypeDefinition type = null,
            Pawn instigator = null,
            UnityEngine.Object source = null,
            PawnTag bodyPart = default,
            Vector3 hitPoint = default,
            Vector3 hitNormal = default)
        {
            Amount = amount;
            Type = type;
            Instigator = instigator;
            Source = source;
            BodyPart = bodyPart;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
        }

        /// <summary>Returns a copy with a different <see cref="Amount"/> and everything else preserved.</summary>
        /// <param name="amount">The new amount.</param>
        /// <remarks>The standard move for an <see cref="IDamageModifier"/>: scale or clamp the amount, keep the provenance.</remarks>
        public DamageInfo WithAmount(float amount) =>
            new DamageInfo(amount, Type, Instigator, Source, BodyPart, HitPoint, HitNormal);

        /// <summary>Returns a copy with a different <see cref="BodyPart"/> and everything else preserved.</summary>
        /// <param name="bodyPart">The hit zone that was struck.</param>
        public DamageInfo WithBodyPart(PawnTag bodyPart) =>
            new DamageInfo(Amount, Type, Instigator, Source, bodyPart, HitPoint, HitNormal);

        /// <summary><c>true</c> when this damage bypasses <see cref="Pawn.IsInvulnerable"/>.</summary>
        public bool IgnoresInvulnerability => Type != null && Type.IgnoresInvulnerability;
    }
}
