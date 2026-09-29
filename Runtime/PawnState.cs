namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// Where a <see cref="Pawn"/> or an <see cref="Actor"/> is in its lifecycle. Exactly one state
    /// at a time, and every transition goes through a method on the owning component — nothing sets
    /// this directly.
    /// </summary>
    /// <remarks>
    /// The legal transitions (identical for both types):
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
    /// A pooled pawn or actor cycles <c>Despawned → Alive → Dead → Despawned</c> forever without
    /// ever being destroyed. <see cref="Incapacitated"/> is opt-in per <c>CanBeIncapacitated</c> on
    /// the definition and sits between <see cref="Alive"/> and <see cref="Dead"/> — most never pass
    /// through it.
    /// </remarks>
    public enum PawnState
    {
        /// <summary>Built but not yet in play. Cannot be damaged or killed.</summary>
        Unspawned = 0,

        /// <summary>In play. The only state in which it can be damaged or killed (and, for a <see cref="Pawn"/>, possessed).</summary>
        Alive = 1,

        /// <summary>In play but defeated. Still in the world and still addressable — a corpse or wreck can be looted, revived or despawned.</summary>
        Dead = 2,

        /// <summary>Taken out of play. The GameObject is deactivated and it is waiting to be destroyed, pooled or spawned again.</summary>
        Despawned = 3,

        /// <summary>
        /// Downed but not dead — a fatal blow landed on something whose definition has
        /// <c>CanBeIncapacitated</c> set. Any further damage, a bleed-out timer, or <c>Kill</c>
        /// finishes it into <see cref="Dead"/>; <c>TryRevive</c> also recovers it back to
        /// <see cref="Alive"/>.
        /// </summary>
        Incapacitated = 4
    }
}
