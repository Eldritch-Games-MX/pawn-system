namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// Whatever is driving a <see cref="Pawn"/> — a player's controller, an AI brain, a replay,
    /// a network proxy, a possessing ghost. The pawn never learns which.
    /// </summary>
    /// <remarks>
    /// Possession is symmetric on purpose: player control and AI control are the same operation
    /// with a different implementation, so a body can change hands mid-game without either side
    /// knowing what the other is.
    /// <code>
    /// public sealed class WanderingBrain : IPawnPossessor
    /// {
    ///     private Pawn pawn;
    ///
    ///     public void OnPossessed(Pawn pawn) => this.pawn = pawn;
    ///     public void OnReleased(Pawn pawn)  => this.pawn = null;
    ///
    ///     public void Tick(float amount)
    ///     {
    ///         if (pawn != null &amp;&amp; pawn.TryGet&lt;IPawnMotor&gt;(out var motor))
    ///             motor.Move(Vector3.forward * 2f, amount);
    ///     }
    /// }
    ///
    /// pawn.TryPossess(brain);   // AI takes the body
    /// pawn.Release();           // and hands it back
    /// </code>
    /// <para>
    /// Both callbacks are raised by the pawn and only by the pawn. Never call them yourself, and
    /// never assume they pair up across a scene reload — a possessor must tolerate
    /// <see cref="OnReleased"/> arriving for a pawn that is mid-death or already despawning.
    /// </para>
    /// </remarks>
    public interface IPawnPossessor
    {
        /// <summary>
        /// Raised after this possessor has taken <paramref name="pawn"/>, once
        /// <see cref="Pawn.CurrentPossessor"/> already points here.
        /// </summary>
        /// <param name="pawn">The pawn now being driven. Never <c>null</c>.</param>
        void OnPossessed(Pawn pawn);

        /// <summary>
        /// Raised after this possessor has given up <paramref name="pawn"/> — by an explicit
        /// <see cref="Pawn.Release"/>, by being swapped out through <see cref="Pawn.Possess"/>,
        /// or automatically on death or despawn.
        /// </summary>
        /// <param name="pawn">The pawn no longer being driven. Never <c>null</c>.</param>
        /// <remarks>
        /// <see cref="Pawn.CurrentPossessor"/> is already <c>null</c> (or the new possessor) when
        /// this runs, so stop ticking and drop the reference here.
        /// </remarks>
        void OnReleased(Pawn pawn);
    }
}
