using System;
using System.Collections.Generic;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// A pawn's secondary resources — mana, stamina, ammunition — each keyed by its
    /// <see cref="ResourceDefinition"/> asset. Empty until something registers a resource.
    /// </summary>
    /// <remarks>
    /// Where <see cref="Pawn.Vitals"/> is the one pool a pawn dies without, a resource pool is any
    /// number of other pools that never kill it — spending mana to zero just means the next spell
    /// fails to cast. Nothing here knows what a resource means; the game decides that entirely
    /// through which <see cref="ResourceDefinition"/> assets it registers.
    /// <code>
    /// pawn.Resources.Register(mana);                 // fresh, full, from the definition's DefaultMax
    /// pawn.Resources.ApplyDelta(mana, -15f);          // cast a spell
    /// if (pawn.Resources.GetCurrent(mana) &gt;= cost) { /* can afford it */ }
    /// </code>
    /// Reuses <see cref="Health"/> internally for each pool's clamping, so a resource pool has the
    /// same tested clamp-and-notify behaviour as vitals, without depending on anything else.
    /// </remarks>
    public sealed class PawnResourcePool
    {
        private readonly Dictionary<ResourceDefinition, Health> pools = new Dictionary<ResourceDefinition, Health>();

        /// <summary>Raised when a registered resource's value actually changes, with the resource, its new current value and its max, in that order.</summary>
        public event Action<ResourceDefinition, float, float> Changed;

        /// <summary>Every resource currently registered on this pawn.</summary>
        public IEnumerable<ResourceDefinition> RegisteredResources => pools.Keys;

        /// <summary>Whether this pawn has the given resource registered at all.</summary>
        /// <param name="definition">The resource to look for.</param>
        public bool IsRegistered(ResourceDefinition definition) => definition != null && pools.ContainsKey(definition);

        /// <summary>Registers a resource at its definition's <see cref="ResourceDefinition.DefaultMax"/>, full. Idempotent — already registered is a no-op.</summary>
        /// <param name="definition">The resource to register.</param>
        /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <c>null</c>.</exception>
        public void Register(ResourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (pools.ContainsKey(definition)) return;

            RegisterInternal(definition, definition.DefaultMax, definition.DefaultMax);
        }

        /// <summary>Registers a resource with an explicit maximum and starting value, overriding the definition's default. Idempotent.</summary>
        /// <param name="definition">The resource to register.</param>
        /// <param name="max">The maximum for this pawn.</param>
        /// <param name="current">The starting value. Defaults to full when omitted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <c>null</c>.</exception>
        public void Register(ResourceDefinition definition, float max, float? current = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (pools.ContainsKey(definition)) return;

            RegisterInternal(definition, max, current ?? max);
        }

        /// <summary>Registers a resource with exact values, overwriting it if already registered.</summary>
        /// <param name="definition">The resource to set.</param>
        /// <param name="max">The maximum to set.</param>
        /// <param name="current">The current value to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Unlike <see cref="Register(ResourceDefinition,float,System.Nullable{float})"/>, this is
        /// not idempotent — it always applies the given values, even over a pool a spawn already
        /// registered. What <see cref="Persistence.PawnPersistence.RestoreState"/> uses, since by
        /// the time it runs, <see cref="Pawn.Spawn()"/> has typically already registered this
        /// pawn's <see cref="Identity.PawnDefinition.InitialResources"/> at their defaults.
        /// </remarks>
        public void Restore(ResourceDefinition definition, float max, float current)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            RegisterInternal(definition, max, current);
        }

        /// <summary>Registers the resource if new, or refills an existing one to its definition's <see cref="ResourceDefinition.DefaultMax"/>.</summary>
        /// <param name="definition">The resource to register or refill.</param>
        /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <c>null</c>.</exception>
        /// <remarks>What <see cref="Pawn.Spawn()"/> calls for every entry in <see cref="Identity.PawnDefinition.InitialResources"/>, so a respawn comes back full.</remarks>
        public void RegisterOrRefill(ResourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (pools.TryGetValue(definition, out Health existing)) existing.SetMax(definition.DefaultMax, fillToMax: true);
            else RegisterInternal(definition, definition.DefaultMax, definition.DefaultMax);
        }

        /// <summary>The resource's current value.</summary>
        /// <param name="definition">The resource to read.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public float GetCurrent(ResourceDefinition definition) => RequirePool(definition).Current;

        /// <summary>The resource's maximum value.</summary>
        /// <param name="definition">The resource to read.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public float GetMax(ResourceDefinition definition) => RequirePool(definition).Max;

        /// <summary>Whether the resource has been spent to zero.</summary>
        /// <param name="definition">The resource to check.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public bool IsDepleted(ResourceDefinition definition) => RequirePool(definition).IsDepleted;

        /// <summary>Adds <paramref name="delta"/> (negative to spend) and returns how much was actually applied after clamping.</summary>
        /// <param name="definition">The resource to change.</param>
        /// <param name="delta">The signed change to apply.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public float ApplyDelta(ResourceDefinition definition, float delta) => RequirePool(definition).ApplyDelta(delta);

        /// <summary>Changes the resource's maximum, clamping the current value into the new range.</summary>
        /// <param name="definition">The resource to change.</param>
        /// <param name="max">The new maximum. Must be greater than zero.</param>
        /// <param name="fillToMax">When <c>true</c>, sets the current value to the new maximum.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public void SetMax(ResourceDefinition definition, float max, bool fillToMax = false) => RequirePool(definition).SetMax(max, fillToMax);

        /// <summary>Sets the resource's current value to a fraction of its maximum.</summary>
        /// <param name="definition">The resource to change.</param>
        /// <param name="fraction">A fraction in <c>[0, 1]</c>; values outside are clamped.</param>
        /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
        public void Fill(ResourceDefinition definition, float fraction) => RequirePool(definition).Fill(fraction);

        private void RegisterInternal(ResourceDefinition definition, float max, float current)
        {
            var pool = new Health(current, max);
            pool.Changed += (currentValue, maxValue) => Changed?.Invoke(definition, currentValue, maxValue);
            pools[definition] = pool;
        }

        private Health RequirePool(ResourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!pools.TryGetValue(definition, out Health pool))
                throw new InvalidOperationException(
                    $"Resource '{definition.name}' is not registered on this pool. Call Register first.");
            return pool;
        }
    }
}
