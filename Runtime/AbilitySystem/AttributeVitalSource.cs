using System;
using EldritchGames.AbilitySystem;
using EldritchGames.AbilitySystem.Definitions;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.AbilitySystem
{
    /// <summary>
    /// An <see cref="IVitalSource"/> backed by an attribute on an
    /// <see cref="AbilitySystemComponent"/>, so a pawn's health and the ability system's health
    /// are the same number instead of two that drift apart.
    /// </summary>
    /// <remarks>
    /// Without this, a project using both packages has a pawn losing vitals to
    /// <see cref="Pawn.ApplyDamage"/> while abilities drain an unrelated attribute. With it, damage
    /// modifiers, costs, effects and regeneration all move one value, and the pawn still dies at
    /// zero.
    /// <code>
    /// // in Awake, before the pawn spawns
    /// pawn.SetVitalSource(new AttributeVitalSource(abilitySystem, healthAttribute, maxHealthAttribute));
    /// </code>
    /// <see cref="AbilityVitalsBinder"/> does this wiring from the inspector.
    /// </remarks>
    public sealed class AttributeVitalSource : IVitalSource
    {
        private readonly AbilitySystemComponent component;
        private readonly AttributeDefinition attribute;
        private readonly AttributeDefinition maxAttribute;

        /// <summary>Binds a pawn's vitals to an ability-system attribute.</summary>
        /// <param name="component">The component owning the attribute.</param>
        /// <param name="attribute">The attribute holding the current value.</param>
        /// <param name="maxAttribute">
        /// Optional attribute holding the maximum, for games where buffs raise max health. When
        /// omitted, <see cref="AttributeDefinition.MaxValue"/> is used and must be finite.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> or <paramref name="attribute"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The attribute is not registered on the component.</exception>
        /// <exception cref="ArgumentException">No maximum attribute was given and the attribute has no finite maximum.</exception>
        public AttributeVitalSource(
            AbilitySystemComponent component,
            AttributeDefinition attribute,
            AttributeDefinition maxAttribute = null)
        {
            this.component = component != null ? component : throw new ArgumentNullException(nameof(component));
            this.attribute = attribute != null ? attribute : throw new ArgumentNullException(nameof(attribute));
            this.maxAttribute = maxAttribute;

            if (!component.Attributes.IsRegistered(attribute))
                throw new InvalidOperationException(
                    $"Attribute '{attribute.name}' is not registered on this AbilitySystemComponent. Add it to the component's Initial Attributes list, or call RegisterAttribute before binding a pawn's vitals to it.");

            if (maxAttribute == null && float.IsInfinity(attribute.MaxValue))
                throw new ArgumentException(
                    $"Attribute '{attribute.name}' has no finite Max Value, so it cannot act as a pawn's vitals on its own. Set a Max Value on the attribute, or pass a separate max attribute.",
                    nameof(attribute));

            if (maxAttribute != null && !component.Attributes.IsRegistered(maxAttribute))
                throw new InvalidOperationException(
                    $"Attribute '{maxAttribute.name}' is not registered on this AbilitySystemComponent. Add it to the component's Initial Attributes list, or call RegisterAttribute before binding a pawn's vitals to it.");

            component.Attributes.AttributeChanged += OnAttributeChanged;
        }

        /// <inheritdoc/>
        public float Current => component.Attributes.GetCurrentValue(attribute);

        /// <inheritdoc/>
        public float Max => maxAttribute != null ? component.Attributes.GetCurrentValue(maxAttribute) : attribute.MaxValue;

        /// <inheritdoc/>
        public bool IsDepleted => Current <= 0f;

        /// <inheritdoc/>
        /// <remarks>Raised whenever the backing attribute changes, whoever changed it — damage, an ability cost, or a regeneration effect.</remarks>
        public event Action<float, float> Changed;

        /// <inheritdoc/>
        /// <remarks>Applied to the attribute's base value, so existing modifiers keep applying on top.</remarks>
        public float ApplyDelta(float delta)
        {
            if (delta == 0f) return 0f;

            float previous = Current;
            component.Attributes.ModifyBaseValue(attribute, delta);
            return Current - previous;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">
        /// This source has no max attribute, so the maximum is the attribute asset's own clamp and
        /// cannot be changed at runtime.
        /// </exception>
        public void SetMax(float max, bool fillToMax = false)
        {
            if (maxAttribute == null)
                throw new InvalidOperationException(
                    $"The maximum of '{attribute.name}' comes from the attribute asset and cannot be changed at runtime. Pass a max attribute to AttributeVitalSource if the maximum needs to move.");

            component.Attributes.SetBaseValue(maxAttribute, max);
            if (fillToMax) Fill(1f);
        }

        /// <inheritdoc/>
        public void Fill(float fraction)
        {
            component.Attributes.SetBaseValue(attribute, Mathf.Clamp01(fraction) * Max);
        }

        /// <summary>Stops listening to the ability system. Call when the pawn is destroyed.</summary>
        /// <remarks><see cref="AbilityVitalsBinder"/> calls this for you.</remarks>
        public void Dispose()
        {
            component.Attributes.AttributeChanged -= OnAttributeChanged;
        }

        private void OnAttributeChanged(AttributeDefinition changed, float oldValue, float newValue)
        {
            if (changed != attribute && changed != maxAttribute) return;
            Changed?.Invoke(Current, Max);
        }
    }
}
