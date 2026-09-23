namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// Where a <see cref="Pawn"/> is in its lifecycle. Exactly one state at a time, and every
    /// transition goes through a method on <see cref="Pawn"/> — nothing sets this directly.
    /// </summary>
    /// <remarks>
    /// The legal transitions:
    /// <code>
    /// Unspawned ──Spawn()──► Alive ──Die()/Kill()───────────────► Dead
    ///                          │  ▲                                ▲
    ///                          │  └──────── TryRevive() ───────────┤
    ///                          │                                   │
    ///                          ▼ (fatal blow, CanBeIncapacitated)   │
    ///                     Incapacitated ──Die()/Kill()/bleeds out──┘
    ///
    /// Alive, Dead or Incapacitated ──Despawn()──► Despawned ──Spawn()──► Alive
    /// </code>
    /// A pooled pawn cycles <c>Despawned → Alive → Dead → Despawned</c> forever without ever
    /// being destroyed. <see cref="Incapacitated"/> is opt-in per <see cref="Identity.PawnDefinition.CanBeIncapacitated"/>
    /// and sits between <see cref="Alive"/> and <see cref="Dead"/> — most pawns never pass through it.
    /// </remarks>
    public enum PawnState
    {
        /// <summary>Built but not yet in play. Cannot be possessed, damaged, or killed.</summary>
        Unspawned = 0,

        /// <summary>In play. The only state in which a pawn can be possessed, damaged or killed.</summary>
        Alive = 1,

        /// <summary>In play but defeated. Still in the world and still addressable — corpses can be looted, revived or despawned.</summary>
        Dead = 2,

        /// <summary>Taken out of play. The GameObject is deactivated and the pawn is waiting to be destroyed, pooled or spawned again.</summary>
        Despawned = 3,

        /// <summary>
        /// Downed but not dead — a fatal blow landed on a pawn whose <see cref="Identity.PawnDefinition.CanBeIncapacitated"/>
        /// is set. Any further damage, a bleed-out timer, or <see cref="Pawn.Kill"/> finishes it
        /// into <see cref="Dead"/>; <see cref="Pawn.TryRevive"/> also recovers it back to <see cref="Alive"/>.
        /// </summary>
        Incapacitated = 4
    }
}
