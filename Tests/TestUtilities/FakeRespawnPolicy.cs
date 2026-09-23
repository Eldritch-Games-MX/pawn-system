using EldritchGames.PawnSystem.Lifecycle;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// An <see cref="IRespawnPolicy"/> spy: answers with canned values and counts how often it was
    /// asked.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var policy = new FakeRespawnPolicy(shouldRespawn: false);
    /// spawner.RespawnPolicy = policy;
    /// pawn.Kill();
    /// Assert.AreEqual(0, spawner.PendingRespawnCount, "A refused respawn must not be queued.");
    /// </code>
    /// </remarks>
    public sealed class FakeRespawnPolicy : IRespawnPolicy
    {
        private readonly bool shouldRespawn;
        private readonly float delay;

        /// <summary>Creates a policy with canned answers.</summary>
        /// <param name="shouldRespawn">What <see cref="ShouldRespawn"/> always answers.</param>
        /// <param name="delay">What <see cref="GetDelay"/> always answers.</param>
        public FakeRespawnPolicy(bool shouldRespawn = true, float delay = 0f)
        {
            this.shouldRespawn = shouldRespawn;
            this.delay = delay;
        }

        /// <summary>How many times <see cref="ShouldRespawn"/> was called.</summary>
        public int ShouldRespawnCallCount { get; private set; }

        /// <summary>How many times <see cref="GetDelay"/> was called.</summary>
        public int GetDelayCallCount { get; private set; }

        /// <inheritdoc/>
        public bool ShouldRespawn(Pawn pawn)
        {
            ShouldRespawnCallCount++;
            return shouldRespawn;
        }

        /// <inheritdoc/>
        public float GetDelay(Pawn pawn)
        {
            GetDelayCallCount++;
            return delay;
        }
    }
}
