namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// Anything that can receive a <see cref="DamageInfo"/> — a <see cref="Pawn"/>, a hit zone on
    /// one (<see cref="DamageReceiver"/>), or a destructible prop that is not a pawn at all.
    /// </summary>
    /// <remarks>
    /// Weapons, abilities and traps should target this interface rather than <see cref="Pawn"/>,
    /// so the same projectile can hurt a character and a barrel:
    /// <code>
    /// if (hit.collider.TryGetComponent&lt;IDamageable&gt;(out var target))
    ///     target.ApplyDamage(new DamageInfo(25f, fireType, instigator: shooter));
    /// </code>
    /// </remarks>
    public interface IDamageable
    {
        /// <summary>
        /// Applies <paramref name="damage"/> and returns how much was actually taken after
        /// modifiers, clamping and invulnerability.
        /// </summary>
        /// <param name="damage">The incoming damage event.</param>
        /// <returns>
        /// The amount actually removed. Zero means the damage was fully absorbed, ignored, or
        /// arrived at a target that cannot be hurt right now — implementations must never throw
        /// for that case.
        /// </returns>
        float ApplyDamage(in DamageInfo damage);
    }
}
