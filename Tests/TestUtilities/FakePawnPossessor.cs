namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// An <see cref="IPawnPossessor"/> spy: records how often it was given and taken off a pawn,
    /// and which pawn it holds.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var possessor = new FakePawnPossessor();
    /// pawn.TryPossess(possessor);
    /// pawn.Kill();
    /// Assert.AreEqual(1, possessor.ReleasedCount, "Death must release the possessor.");
    /// </code>
    /// </remarks>
    public sealed class FakePawnPossessor : IPawnPossessor
    {
        /// <summary>How many times this possessor has been given a pawn.</summary>
        public int PossessedCount { get; private set; }

        /// <summary>How many times this possessor has had a pawn taken away.</summary>
        public int ReleasedCount { get; private set; }

        /// <summary>The pawn currently held, or <c>null</c> when between bodies.</summary>
        public Pawn CurrentPawn { get; private set; }

        /// <summary>The last pawn this possessor was released from, whether or not it holds one now.</summary>
        public Pawn LastReleasedPawn { get; private set; }

        /// <inheritdoc/>
        public void OnPossessed(Pawn pawn)
        {
            PossessedCount++;
            CurrentPawn = pawn;
        }

        /// <inheritdoc/>
        public void OnReleased(Pawn pawn)
        {
            ReleasedCount++;
            LastReleasedPawn = pawn;
            CurrentPawn = null;
        }
    }
}
