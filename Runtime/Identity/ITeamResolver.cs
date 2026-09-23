namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// Decides how one pawn regards another. Replace the default to express reputation,
    /// temporary alliances, mind control or free-for-all rules without touching this package.
    /// </summary>
    /// <remarks>
    /// Assign per pawn through <see cref="Pawn.TeamResolver"/>; the default is
    /// <see cref="DefaultTeamResolver"/>, which reads the lists on <see cref="TeamDefinition"/>.
    /// <code>
    /// public sealed class DeathmatchResolver : ITeamResolver
    /// {
    ///     public PawnRelationship Resolve(Pawn subject, Pawn other)
    ///         => subject == other ? PawnRelationship.Friendly : PawnRelationship.Hostile;
    /// }
    ///
    /// pawn.TeamResolver = new DeathmatchResolver();
    /// </code>
    /// </remarks>
    public interface ITeamResolver
    {
        /// <summary>
        /// Returns how <paramref name="subject"/> regards <paramref name="other"/>.
        /// </summary>
        /// <param name="subject">The pawn doing the regarding.</param>
        /// <param name="other">The pawn being regarded.</param>
        /// <returns>
        /// The relationship. Implementations must tolerate a <c>null</c> argument and a pawn with
        /// no team, answering <see cref="PawnRelationship.Neutral"/> rather than throwing.
        /// </returns>
        PawnRelationship Resolve(Pawn subject, Pawn other);
    }
}
