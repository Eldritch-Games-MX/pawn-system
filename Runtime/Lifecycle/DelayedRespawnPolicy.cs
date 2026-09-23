namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// The built-in <see cref="IRespawnPolicy"/>: waits a fixed delay, optionally only for a
    /// limited number of respawns.
    /// </summary>
    /// <remarks>
    /// Covers the two common cases — "back in five seconds, forever" and "three lives, then you
    /// stay dead".
    /// <code>
    /// spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 5f);              // endless
    /// spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 2f, maxRespawns: 3); // three lives
    /// </code>
    /// The count is per policy instance, not per pawn, so give each player their own if lives are
    /// individual.
    /// </remarks>
    public sealed class DelayedRespawnPolicy : IRespawnPolicy
    {
        private readonly float delay;
        private readonly int maxRespawns;
        private int used;

        /// <summary>Creates a fixed-delay policy.</summary>
        /// <param name="delay">How long to wait, in the spawner's tick units. Negative values are treated as zero.</param>
        /// <param name="maxRespawns">How many respawns to allow in total, or a negative value for unlimited.</param>
        public DelayedRespawnPolicy(float delay, int maxRespawns = -1)
        {
            this.delay = delay < 0f ? 0f : delay;
            this.maxRespawns = maxRespawns;
        }

        /// <summary>How many respawns this policy has granted so far.</summary>
        public int Used => used;

        /// <summary>How many respawns remain, or <c>-1</c> when unlimited.</summary>
        public int Remaining => maxRespawns < 0 ? -1 : maxRespawns - used;

        /// <inheritdoc/>
        /// <remarks>Counts a granted respawn, so a policy with a limit refuses once the limit is reached.</remarks>
        public bool ShouldRespawn(Pawn pawn)
        {
            if (maxRespawns < 0) return true;
            if (used >= maxRespawns) return false;

            used++;
            return true;
        }

        /// <inheritdoc/>
        public float GetDelay(Pawn pawn) => delay;
    }
}
