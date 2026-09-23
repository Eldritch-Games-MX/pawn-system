using EldritchGames.InputSystem;

namespace EldritchGames.PawnSystem.InputSystem
{
    /// <summary>
    /// An <see cref="IPawnController"/> that swallows everything. Given to a controller that is
    /// between bodies, so it always has something to talk to.
    /// </summary>
    /// <remarks>
    /// <see cref="ControllerPossessor"/> hands this to the controller on release, and it is also
    /// the right thing to pass to <c>IController.Initialize</c> before the player has a pawn:
    /// <code>
    /// controller.Initialize(playerIndex, NullPawnController.Instance, stack);
    /// </code>
    /// It has no state, so one shared instance serves every controller.
    /// </remarks>
    public sealed class NullPawnController : IPawnController
    {
        /// <summary>The shared instance. Stateless, so sharing is safe.</summary>
        public static readonly NullPawnController Instance = new NullPawnController();

        private NullPawnController()
        {
        }

        /// <summary>Discards <paramref name="command"/>.</summary>
        /// <param name="command">The command to discard.</param>
        public void ExecuteCommand(ICommand command)
        {
        }

        /// <summary>Always reports that the capability is missing.</summary>
        /// <typeparam name="T">The capability interface being asked for.</typeparam>
        /// <param name="capability">Always <c>null</c>.</param>
        /// <returns>Always <c>false</c>.</returns>
        public bool TryGet<T>(out T capability) where T : class
        {
            capability = null;
            return false;
        }
    }
}
