namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// One step in a pawn's damage pipeline — armour, a resistance, a difficulty multiplier, a
    /// head-shot bonus, a damage cap. Receives the incoming damage and returns the damage that
    /// should continue down the pipeline.
    /// </summary>
    /// <remarks>
    /// Modifiers run in a defined order: first the ones authored on
    /// <see cref="Identity.PawnDefinition.DamageModifiers"/> (top to bottom in the inspector), then any
    /// <c>IDamageModifier</c> components on the pawn's GameObject in component order. Order is
    /// part of the contract — a flat reduction before a percentage reads very differently from
    /// the reverse.
    /// <code>
    /// [System.Serializable]
    /// [PawnDoc("Halves damage of a given type.")]
    /// public sealed class ResistanceModifier : IDamageModifier
    /// {
    ///     [SerializeField] private DamageTypeDefinition damageType;
    ///
    ///     public DamageInfo Modify(Pawn target, DamageInfo damage)
    ///         => damage.Type == damageType ? damage.WithAmount(damage.Amount * 0.5f) : damage;
    /// }
    /// </code>
    /// Implementations must be pure with respect to the pawn: return a modified copy, never
    /// apply damage, kill, or mutate the target from inside <see cref="Modify"/>.
    /// </remarks>
    public interface IDamageModifier
    {
        /// <summary>
        /// Returns the damage that should continue down the pipeline, given the incoming
        /// <paramref name="damage"/> and the pawn about to receive it.
        /// </summary>
        /// <param name="target">The pawn the damage is headed for. Never <c>null</c>.</param>
        /// <param name="damage">The damage as modified by every earlier step.</param>
        /// <returns>
        /// The modified damage. Return <paramref name="damage"/> unchanged to opt out; return
        /// <c>damage.WithAmount(0f)</c> to absorb it entirely — the pipeline still runs to the end.
        /// </returns>
        DamageInfo Modify(Pawn target, DamageInfo damage);
    }
}
