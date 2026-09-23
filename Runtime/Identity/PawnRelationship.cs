namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// How one <see cref="Pawn"/> regards another, as resolved by an <see cref="ITeamResolver"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately three-valued and nothing more. Games that need reputation scores, temporary
    /// truces or per-pawn grudges implement their own <see cref="ITeamResolver"/> and collapse
    /// their model onto these three answers at the moment a system asks.
    /// </remarks>
    public enum PawnRelationship
    {
        /// <summary>No opinion — neither an ally nor a valid target.</summary>
        Neutral = 0,

        /// <summary>An ally. Friendly fire, healing and buff targeting decisions read this.</summary>
        Friendly = 1,

        /// <summary>An enemy. AI target selection and hostile ability filters read this.</summary>
        Hostile = 2
    }
}
