namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// The outcome of <see cref="Pawn.TryRevive"/> or <see cref="Actor.TryRevive"/>. Every failure
    /// leaves it as it was and raises no events.
    /// </summary>
    public enum ReviveResult
    {
        /// <summary>It is alive again: vitals restored, state <see cref="PawnState.Alive"/>, <see cref="Pawn.Revived"/>/<see cref="Actor.Revived"/> raised.</summary>
        Success = 0,

        /// <summary>It is neither <see cref="PawnState.Dead"/> nor <see cref="PawnState.Incapacitated"/>, so there is nothing to revive.</summary>
        NotDead = 1,

        /// <summary>It is <see cref="PawnState.Despawned"/>. Spawn it instead — spawning already restores vitals.</summary>
        NotSpawned = 2
    }
}
