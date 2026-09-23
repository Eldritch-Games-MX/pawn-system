using System;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// The clamped number a pawn dies when it runs out of — health, hull integrity, sanity,
    /// willpower. A pure quantity with no opinion about damage, death or revival.
    /// </summary>
    /// <remarks>
    /// The seam exists so a pawn's vitals can live somewhere other than this package. The default
    /// is <see cref="Health"/>, a plain C# object; a project using the Eldritch Ability System can
    /// swap in an implementation backed by an <c>AttributeSet</c> so the ability pipeline and the
    /// pawn share one source of truth instead of two that drift.
    /// <code>
    /// pawn.SetVitalSource(new AttributeVitalSource(abilitySystemComponent, healthAttribute));
    /// </code>
    /// <para>
    /// Rules, invulnerability, death and revival all live on <see cref="Pawn"/>, never here — an
    /// implementation only has to clamp a number and report when it changes.
    /// </para>
    /// </remarks>
    public interface IVitalSource
    {
        /// <summary>The current value, always within <c>[0, <see cref="Max"/>]</c>.</summary>
        float Current { get; }

        /// <summary>The upper bound. Always greater than zero.</summary>
        float Max { get; }

        /// <summary><c>true</c> when <see cref="Current"/> has reached zero.</summary>
        bool IsDepleted { get; }

        /// <summary>
        /// Raised after any change, with the new current value and the maximum, in that order.
        /// </summary>
        event Action<float, float> Changed;

        /// <summary>
        /// Adds <paramref name="delta"/> (negative to remove) and returns how much was actually
        /// applied after clamping.
        /// </summary>
        /// <param name="delta">The signed change to apply.</param>
        /// <returns>
        /// The signed amount actually applied. Removing 30 from a value of 10 returns <c>-10</c>,
        /// so the caller can compute overkill as <c>delta - applied</c>. Raises
        /// <see cref="Changed"/> only when the value actually moved.
        /// </returns>
        float ApplyDelta(float delta);

        /// <summary>
        /// Changes the maximum, clamping the current value into the new range.
        /// </summary>
        /// <param name="max">The new maximum. Values at or below zero are rejected by implementations.</param>
        /// <param name="fillToMax">When <c>true</c>, the current value is set to the new maximum.</param>
        void SetMax(float max, bool fillToMax = false);

        /// <summary>
        /// Sets the current value to <paramref name="fraction"/> of <see cref="Max"/>.
        /// </summary>
        /// <param name="fraction">A fraction in <c>[0, 1]</c>; values outside are clamped.</param>
        /// <remarks>Used by <see cref="Pawn.Spawn()"/> and <see cref="Pawn.TryRevive"/> to reset a pawn.</remarks>
        void Fill(float fraction);
    }
}
