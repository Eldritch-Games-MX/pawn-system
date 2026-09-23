namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// Where a spawner gets pawn instances from and gives them back to. Implement it to reuse
    /// instances instead of instantiating and destroying — or leave it unset and the spawner will
    /// instantiate and destroy.
    /// </summary>
    /// <remarks>
    /// Pooling is a capability, not a requirement: a game that spawns a boss once should not carry
    /// a pool, and a wave shooter should. Assign through <see cref="PawnSpawner.Pool"/>.
    /// <code>
    /// spawner.Pool = new SimplePawnPool(transform);   // parented under the spawner
    /// </code>
    /// <para>
    /// A pooled pawn is spawned and despawned repeatedly, never destroyed, so it cycles
    /// <see cref="PawnState.Despawned"/> → <see cref="PawnState.Alive"/> → … indefinitely. An
    /// implementation must return instances in a state where <see cref="Pawn.Spawn()"/> will
    /// succeed — which means despawned or unspawned, not dead.
    /// </para>
    /// </remarks>
    public interface IPawnPool
    {
        /// <summary>Produces an instance of <paramref name="prefab"/>, reusing one when possible.</summary>
        /// <param name="prefab">The pawn prefab to instantiate or reuse.</param>
        /// <returns>An instance ready to be spawned, or <c>null</c> when the pool refuses.</returns>
        Pawn Rent(Pawn prefab);

        /// <summary>Takes <paramref name="pawn"/> back for reuse.</summary>
        /// <param name="pawn">The instance being returned. Already despawned by the caller.</param>
        void Return(Pawn pawn);
    }
}
