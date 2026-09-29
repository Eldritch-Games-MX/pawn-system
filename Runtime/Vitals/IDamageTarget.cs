using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// The minimal surface an <see cref="IDamageModifier"/> needs from whatever is about to take a
    /// hit — implemented by both <see cref="Pawn"/> and <see cref="Actor"/> so a resistance,
    /// falloff-by-distance or health-aware modifier is written once and works against either,
    /// without the damage pipeline caring whether the receiver can be possessed.
    /// </summary>
    public interface IDamageTarget
    {
        /// <summary>Where the target is, for a modifier that cares about distance or facing.</summary>
        Transform Transform { get; }

        /// <summary>The target's health pool, for a modifier that scales with missing or current vitals.</summary>
        IVitalSource Vitals { get; }
    }
}
