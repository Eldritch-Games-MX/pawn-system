using EldritchGames.InputSystem;
using UnityEngine;

namespace EldritchGames.PawnSystem.InputSystem
{
    /// <summary>
    /// The bridge an <see cref="IController"/> drives instead of touching the <see cref="Pawn"/>
    /// directly: an <see cref="IPawnController"/> that forwards commands to the pawn's
    /// capabilities and drops them when the pawn is dead or unpossessed.
    /// </summary>
    /// <remarks>
    /// Add one component to any pawn a player should be able to drive; the rest of the wiring is
    /// <see cref="ControllerPossessor"/>.
    /// <code>
    /// // once, at setup
    /// controller.Initialize(playerIndex, NullPawnController.Instance, new InputContextStack());
    /// var possessor = new ControllerPossessor(controller);
    ///
    /// // whenever the player takes a body
    /// pawn.TryPossess(possessor);
    /// </code>
    /// Commands reach the pawn through <see cref="Pawn.TryGet{T}"/>, so a command that asks for a
    /// capability this pawn does not have silently does nothing — the same graceful-degradation
    /// contract as <c>ICommand</c> everywhere else in the Eldritch ecosystem.
    /// </remarks>
    public sealed class PawnInputTarget : MonoBehaviour, IPawnController
    {
        [Header("Bindings")]
        [Tooltip("The pawn commands are forwarded to. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private Pawn pawn;

        /// <summary>The pawn this target forwards commands to.</summary>
        public Pawn Pawn
        {
            get => pawn;
            set => pawn = value;
        }

        /// <summary>
        /// Runs <paramref name="command"/> against the pawn's capabilities, or drops it when there
        /// is no pawn, the pawn is not alive, or nothing is possessing it.
        /// </summary>
        /// <param name="command">The command collected from the active input context this frame.</param>
        /// <remarks>
        /// Dropping rather than throwing is deliberate: input keeps arriving for a frame or two
        /// after a pawn dies or is released, and that must not be an error.
        /// </remarks>
        public void ExecuteCommand(ICommand command)
        {
            if (command == null) return;
            if (pawn == null || !pawn.IsAlive || !pawn.IsPossessed) return;

            command.Execute(this);
        }

        /// <summary>Forwards a capability query to the pawn.</summary>
        /// <typeparam name="T">The capability interface the command is asking for.</typeparam>
        /// <param name="capability">The capability, or <c>null</c> when the pawn does not have it.</param>
        /// <returns><c>true</c> when the capability was found.</returns>
        public bool TryGet<T>(out T capability) where T : class
        {
            if (pawn == null)
            {
                capability = null;
                return false;
            }

            return pawn.TryGet(out capability);
        }

        private void Awake()
        {
            if (pawn == null) pawn = GetComponentInParent<Pawn>();
        }
    }
}
