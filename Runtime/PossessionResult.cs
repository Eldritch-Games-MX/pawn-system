namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// The outcome of <see cref="Pawn.TryPossess"/>. Every failure leaves the pawn's current
    /// possessor untouched and raises no events.
    /// </summary>
    /// <remarks>
    /// The same codes cover player and AI possession — a pawn never knows which kind of
    /// possessor is asking, which is the whole point of the abstraction.
    /// </remarks>
    public enum PossessionResult
    {
        /// <summary>The possessor took the pawn: <see cref="IPawnPossessor.OnPossessed"/> ran and <see cref="Pawn.Possessed"/> was raised.</summary>
        Success = 0,

        /// <summary>Another possessor holds the pawn. Call <see cref="Pawn.Release"/> first, or use <see cref="Pawn.Possess"/> to swap in one step.</summary>
        AlreadyPossessed = 1,

        /// <summary>The pawn is <see cref="PawnState.Unspawned"/> or <see cref="PawnState.Despawned"/> — it is not in the world to be driven.</summary>
        NotSpawned = 2,

        /// <summary>The pawn is <see cref="PawnState.Dead"/>. Revive it first, or possess a different body.</summary>
        PawnDead = 3,

        /// <summary>The possessor already holds this pawn. Nothing changed and no events were raised.</summary>
        AlreadyOwner = 4
    }
}
