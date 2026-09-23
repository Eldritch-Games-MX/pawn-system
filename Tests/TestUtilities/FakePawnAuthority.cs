namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// An <see cref="IPawnAuthority"/> stub with a settable answer, for tests that check how game
    /// code reacts to authority rather than the package's own (nonexistent) enforcement of it.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var authority = new FakePawnAuthority(hasAuthority: false);
    /// pawn.Authority = authority;
    /// Assert.IsFalse(pawn.HasAuthority);
    /// </code>
    /// </remarks>
    public sealed class FakePawnAuthority : IPawnAuthority
    {
        /// <summary>Creates a stub with a fixed answer.</summary>
        /// <param name="hasAuthority">What <see cref="HasAuthority"/> answers.</param>
        public FakePawnAuthority(bool hasAuthority = true)
        {
            HasAuthority = hasAuthority;
        }

        /// <inheritdoc/>
        public bool HasAuthority { get; set; }
    }
}
