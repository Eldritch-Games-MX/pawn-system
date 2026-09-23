using System;
using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// The built-in <see cref="IDamageModifier"/>: subtracts a flat amount from incoming damage,
    /// optionally only from one damage type. Armour, in its simplest form.
    /// </summary>
    /// <remarks>
    /// Ships as the one minimal implementation so a project has something to drop into
    /// <see cref="Identity.PawnDefinition.DamageModifiers"/> on day one and a worked example to copy when
    /// writing percentage resistances, hit-zone multipliers or difficulty scaling.
    /// <para>
    /// Damage is never pushed below zero, and a fully absorbed hit still travels the rest of the
    /// pipeline and still reaches <see cref="Pawn.DamageTaken"/> with an amount of zero.
    /// </para>
    /// </remarks>
    [Serializable]
    [PawnDoc("Subtracts a flat amount from incoming damage, optionally of one damage type only.")]
    public sealed class FlatDamageReduction : IDamageModifier
    {
        [Tooltip("How much damage to absorb per hit.")]
        [SerializeField] private float reduction = 1f;

        [Tooltip("Only reduce damage of this type. Leave empty to reduce every kind of damage.")]
        [SerializeField] private DamageTypeDefinition damageType;

        /// <summary>How much damage this modifier absorbs per hit.</summary>
        public float Reduction => reduction;

        /// <summary>The only damage type affected, or <c>null</c> when every type is reduced.</summary>
        public DamageTypeDefinition DamageType => damageType;

        /// <inheritdoc/>
        public DamageInfo Modify(Pawn target, DamageInfo damage)
        {
            if (damageType != null && damage.Type != damageType) return damage;

            float reduced = damage.Amount - reduction;
            return damage.WithAmount(reduced < 0f ? 0f : reduced);
        }
    }
}
