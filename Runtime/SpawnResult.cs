namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// The outcome of <see cref="Pawn.Spawn()"/>. Every failure leaves the pawn exactly as it was.
    /// </summary>
    public enum SpawnResult
    {
        /// <summary>The pawn entered play: vitals filled, state <see cref="PawnState.Alive"/>, <see cref="Pawn.Spawned"/> raised.</summary>
        Success = 0,

        /// <summary>The pawn was already <see cref="PawnState.Alive"/> or <see cref="PawnState.Dead"/>. Nothing changed.</summary>
        AlreadySpawned = 1,

        /// <summary>No <see cref="Identity.PawnDefinition"/> is assigned, so the pawn has no vitals, team or identity to spawn with.</summary>
        MissingDefinition = 2
    }
}
