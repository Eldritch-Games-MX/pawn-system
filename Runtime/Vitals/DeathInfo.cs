using EldritchGames.PawnSystem.Identity;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// Why a <see cref="Pawn"/> died: who or what killed it, with what, where it was hit, and by
    /// how much the final blow overshot.
    /// </summary>
    /// <remarks>
    /// Carried by <see cref="Pawn.Died"/> so score, loot, kill feeds, gore and achievements can
    /// all read the same record. Every field is optional — a pawn removed by a cutscene or a
    /// falling-out-of-the-world volume dies with an empty <c>DeathInfo</c>.
    /// <code>
    /// pawn.Died += (dead, info) =>
    /// {
    ///     if (info.Instigator != null) scoreboard.CreditKill(info.Instigator, dead);
    ///     if (info.Overkill > 50f) gore.Gib(dead);
    /// };
    /// </code>
    /// </remarks>
    public readonly struct DeathInfo
    {
        /// <summary>The pawn responsible, or <c>null</c> for environmental and scripted deaths.</summary>
        public Pawn Instigator { get; }

        /// <summary>The weapon, ability asset or trap responsible, or <c>null</c>.</summary>
        public UnityEngine.Object Source { get; }

        /// <summary>The kind of damage that landed the final blow, or <c>null</c>.</summary>
        public DamageTypeDefinition DamageType { get; }

        /// <summary>The hit zone struck by the final blow, or default when it had no location.</summary>
        public PawnTag BodyPart { get; }

        /// <summary>How much of the final blow went past zero. Always zero or positive.</summary>
        public float Overkill { get; }

        /// <summary>Creates a death record. Every argument is optional.</summary>
        /// <param name="instigator">The pawn responsible, or <c>null</c>.</param>
        /// <param name="source">The weapon, ability asset or trap responsible, or <c>null</c>.</param>
        /// <param name="damageType">The kind of damage that landed the final blow, or <c>null</c>.</param>
        /// <param name="bodyPart">The hit zone struck by the final blow.</param>
        /// <param name="overkill">How much damage went past zero.</param>
        public DeathInfo(
            Pawn instigator = null,
            UnityEngine.Object source = null,
            DamageTypeDefinition damageType = null,
            PawnTag bodyPart = default,
            float overkill = 0f)
        {
            Instigator = instigator;
            Source = source;
            DamageType = damageType;
            BodyPart = bodyPart;
            Overkill = overkill < 0f ? 0f : overkill;
        }

        /// <summary>
        /// Builds a death record from the damage that caused it, carrying the provenance across.
        /// </summary>
        /// <param name="damage">The killing damage event.</param>
        /// <param name="overkill">How much of <paramref name="damage"/> went past zero.</param>
        public static DeathInfo FromDamage(in DamageInfo damage, float overkill = 0f) =>
            new DeathInfo(damage.Instigator, damage.Source, damage.Type, damage.BodyPart, overkill);
    }
}
