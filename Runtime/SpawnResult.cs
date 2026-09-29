namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// The outcome of <see cref="Pawn.Spawn()"/> or <see cref="Actor.Spawn()"/>. Every failure leaves
    /// it exactly as it was.
    /// </summary>
    public enum SpawnResult
    {
        /// <summary>It entered play: vitals filled, state <see cref="PawnState.Alive"/>, <see cref="Pawn.Spawned"/>/<see cref="Actor.Spawned"/> raised.</summary>
        Success = 0,

        /// <summary>It was already <see cref="PawnState.Alive"/> or <see cref="PawnState.Dead"/>. Nothing changed.</summary>
        AlreadySpawned = 1,

        /// <summary>No <see cref="Identity.PawnDefinition"/>/<see cref="Identity.ActorDefinition"/> is assigned, so there is no vitals or identity to spawn with.</summary>
        MissingDefinition = 2
    }
}
