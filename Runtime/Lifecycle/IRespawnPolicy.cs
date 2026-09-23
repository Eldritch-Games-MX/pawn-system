namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// Decides whether a dead pawn comes back, and how long it waits. Lives, wave rules, a
    /// hardcore mode with no respawns at all — all of it fits behind these two questions.
    /// </summary>
    /// <remarks>
    /// Assign through <see cref="PawnSpawner.RespawnPolicy"/>; the default is
    /// <see cref="DelayedRespawnPolicy"/>.
    /// <code>
    /// public sealed class LivesPolicy : IRespawnPolicy
    /// {
    ///     private int livesLeft = 3;
    ///
    ///     public bool ShouldRespawn(Pawn pawn) => livesLeft-- > 0;
    ///     public float GetDelay(Pawn pawn) => 2f;
    /// }
    /// </code>
    /// <see cref="ShouldRespawn"/> is asked once per death, at the moment the pawn dies, so a
    /// policy may count calls to implement lives.
    /// </remarks>
    public interface IRespawnPolicy
    {
        /// <summary>Whether <paramref name="pawn"/> should come back at all.</summary>
        /// <param name="pawn">The pawn that just died.</param>
        /// <returns><c>false</c> to leave it dead — the spawner despawns it instead of scheduling a return.</returns>
        bool ShouldRespawn(Pawn pawn);

        /// <summary>How long to wait before respawning <paramref name="pawn"/>.</summary>
        /// <param name="pawn">The pawn waiting to come back.</param>
        /// <returns>
        /// The delay in the spawner's own tick units — seconds for a real-time game, rounds for a
        /// turn-based one. Zero respawns on the next tick.
        /// </returns>
        float GetDelay(Pawn pawn);
    }
}
