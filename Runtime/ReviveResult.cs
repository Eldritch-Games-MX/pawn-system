namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// The outcome of <see cref="Pawn.TryRevive"/>. Every failure leaves the pawn dead and
    /// raises no events.
    /// </summary>
    public enum ReviveResult
    {
        /// <summary>The pawn is alive again: vitals restored, state <see cref="PawnState.Alive"/>, <see cref="Pawn.Revived"/> raised.</summary>
        Success = 0,

        /// <summary>The pawn is not <see cref="PawnState.Dead"/>, so there is nothing to revive.</summary>
        NotDead = 1,

        /// <summary>The pawn is <see cref="PawnState.Despawned"/>. Spawn it instead — spawning already restores vitals.</summary>
        NotSpawned = 2
    }
}
