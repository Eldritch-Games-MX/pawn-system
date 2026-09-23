using System;
using EldritchGames.InputSystem;

namespace EldritchGames.PawnSystem.InputSystem
{
    /// <summary>
    /// Possession for a player: an <see cref="IPawnPossessor"/> that points an
    /// <see cref="IController"/> at whichever pawn it currently holds.
    /// </summary>
    /// <remarks>
    /// This is the whole integration between the two packages. Possessing a pawn points the
    /// controller at that pawn's <see cref="PawnInputTarget"/>; releasing points it at
    /// <see cref="NullPawnController.Instance"/> so input keeps flowing harmlessly into nothing.
    /// <code>
    /// var possessor = new ControllerPossessor(playerController);
    ///
    /// pawn.TryPossess(possessor);      // the player drives this body
    /// otherPawn.Possess(possessor);    // swap bodies: the first is released automatically
    /// otherPawn.Release();             // back to no body
    /// </code>
    /// <para>
    /// An AI possessor is the same call with a different implementation — the pawn cannot tell
    /// them apart, which is what makes a body swappable between a player and a brain mid-game.
    /// </para>
    /// </remarks>
    public sealed class ControllerPossessor : IPawnPossessor
    {
        private readonly IController controller;

        /// <summary>Creates a possessor for <paramref name="controller"/>.</summary>
        /// <param name="controller">The controller to point at possessed pawns. Must already be initialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is <c>null</c>.</exception>
        public ControllerPossessor(IController controller)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <summary>The controller this possessor drives pawns with.</summary>
        public IController Controller => controller;

        /// <summary>The pawn currently held, or <c>null</c> when between bodies.</summary>
        public Pawn CurrentPawn { get; private set; }

        /// <summary>Points the controller at the pawn's <see cref="PawnInputTarget"/>.</summary>
        /// <param name="pawn">The pawn just possessed.</param>
        /// <exception cref="InvalidOperationException">
        /// The pawn has no <see cref="PawnInputTarget"/>, so a controller has nothing to drive.
        /// </exception>
        public void OnPossessed(Pawn pawn)
        {
            PawnInputTarget target = pawn.GetComponentInChildren<PawnInputTarget>(true);
            if (target == null)
                throw new InvalidOperationException(
                    $"Pawn '{pawn.name}' cannot be possessed through the input system because it has no PawnInputTarget component. Add one to the pawn's GameObject.");

            target.Pawn = pawn;
            CurrentPawn = pawn;
            controller.SetPawn(target);
        }

        /// <summary>Points the controller at <see cref="NullPawnController.Instance"/>.</summary>
        /// <param name="pawn">The pawn just released.</param>
        public void OnReleased(Pawn pawn)
        {
            CurrentPawn = null;
            controller.SetPawn(NullPawnController.Instance);
        }
    }
}
