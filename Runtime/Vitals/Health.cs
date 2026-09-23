using System;
using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// The default <see cref="IVitalSource"/>: a clamped float pair with a change event. Plain C#,
    /// no MonoBehaviour, no Unity lifecycle — constructible and fully testable in EditMode.
    /// </summary>
    /// <remarks>
    /// A pawn builds one from its <see cref="Identity.PawnDefinition"/> the first time its vitals are
    /// needed. Construct one directly when you want vitals on something that is not a pawn.
    /// <code>
    /// var health = new Health(current: 50f, max: 100f);
    /// health.Changed += (current, max) => bar.fillAmount = current / max;
    /// health.ApplyDelta(-70f);   // returns -50f, IsDepleted is now true
    /// </code>
    /// </remarks>
    public sealed class Health : IVitalSource
    {
        private float current;
        private float max;

        /// <summary>Creates a vital pool.</summary>
        /// <param name="current">The starting value, clamped into <c>[0, <paramref name="max"/>]</c>.</param>
        /// <param name="max">The maximum. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is zero or negative.</exception>
        public Health(float current, float max)
        {
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max), max, "A vital pool needs a maximum greater than zero.");

            this.max = max;
            this.current = Mathf.Clamp(current, 0f, max);
        }

        /// <summary>Creates a full vital pool.</summary>
        /// <param name="max">The maximum, which is also the starting value. Must be greater than zero.</param>
        public Health(float max) : this(max, max)
        {
        }

        /// <inheritdoc/>
        public float Current => current;

        /// <inheritdoc/>
        public float Max => max;

        /// <inheritdoc/>
        public bool IsDepleted => current <= 0f;

        /// <inheritdoc/>
        public event Action<float, float> Changed;

        /// <inheritdoc/>
        public float ApplyDelta(float delta)
        {
            if (delta == 0f) return 0f;

            float previous = current;
            current = Mathf.Clamp(current + delta, 0f, max);

            float applied = current - previous;
            if (applied != 0f) Changed?.Invoke(current, max);
            return applied;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is zero or negative.</exception>
        public void SetMax(float max, bool fillToMax = false)
        {
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max), max, "A vital pool needs a maximum greater than zero.");

            this.max = max;
            current = fillToMax ? max : Mathf.Clamp(current, 0f, max);
            Changed?.Invoke(current, this.max);
        }

        /// <inheritdoc/>
        public void Fill(float fraction)
        {
            current = Mathf.Clamp01(fraction) * max;
            Changed?.Invoke(current, max);
        }
    }
}
