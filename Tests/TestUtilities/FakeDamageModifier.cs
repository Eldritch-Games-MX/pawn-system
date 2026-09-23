using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Vitals;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// An <see cref="IDamageModifier"/> spy: applies a canned transform to the amount and records
    /// every call, so a test can pin both the result and the pipeline order.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var halve = new FakeDamageModifier(amount => amount * 0.5f, "halve");
    /// var flat  = new FakeDamageModifier(amount => amount - 5f, "flat");
    /// var definition = new PawnDefinitionBuilder().WithDamageModifiers(halve, flat).Build();
    /// // 100 damage lands as 45, and FakeDamageModifier.CallLog records "halve" before "flat".
    /// </code>
    /// </remarks>
    public sealed class FakeDamageModifier : IDamageModifier
    {
        private readonly Func<float, float> transform;

        /// <summary>Creates a spy.</summary>
        /// <param name="transform">How to change the incoming amount. <c>null</c> leaves it alone.</param>
        /// <param name="label">Name recorded in <see cref="CallLog"/>, for order assertions.</param>
        /// <param name="callLog">Shared log so several modifiers can record into one list. A fresh list is created when omitted.</param>
        public FakeDamageModifier(Func<float, float> transform = null, string label = "modifier", List<string> callLog = null)
        {
            this.transform = transform;
            Label = label;
            CallLog = callLog ?? new List<string>();
        }

        /// <summary>The name this modifier records when it runs.</summary>
        public string Label { get; }

        /// <summary>Labels of every modifier that has run, in order, for the shared log this one writes to.</summary>
        public List<string> CallLog { get; }

        /// <summary>How many times <see cref="Modify"/> has been called on this instance.</summary>
        public int ModifyCallCount { get; private set; }

        /// <summary>The pawn passed to the most recent call, or <c>null</c> when it has never run.</summary>
        public Pawn LastTarget { get; private set; }

        /// <inheritdoc/>
        public DamageInfo Modify(Pawn target, DamageInfo damage)
        {
            ModifyCallCount++;
            LastTarget = target;
            CallLog.Add(Label);

            return transform == null ? damage : damage.WithAmount(transform(damage.Amount));
        }
    }
}
