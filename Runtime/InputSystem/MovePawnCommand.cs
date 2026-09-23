using EldritchGames.InputSystem;
using EldritchGames.PawnSystem.Movement;
using UnityEngine;

namespace EldritchGames.PawnSystem.InputSystem
{
    /// <summary>
    /// Moves whatever pawn it is executed against, by asking for an <see cref="IPawnMotor"/> and
    /// doing nothing if there is not one.
    /// </summary>
    /// <remarks>
    /// The worked example of a command that targets a Pawn System capability. Build one per frame
    /// in an input context and let the controller deliver it:
    /// <code>
    /// public override void CollectCommands(IList&lt;ICommand&gt; buffer)
    /// {
    ///     Vector2 stick = moveAction.ReadValue&lt;Vector2&gt;();
    ///     Vector3 velocity = new Vector3(stick.x, 0f, stick.y) * moveSpeed;
    ///     buffer.Add(new MovePawnCommand(velocity, Time.deltaTime));
    /// }
    /// </code>
    /// Speed belongs to the game, not to this command or to the motor — pass a velocity that
    /// already accounts for the pawn's stats, buffs and status effects.
    /// </remarks>
    public sealed class MovePawnCommand : ICommand
    {
        private readonly Vector3 worldVelocity;
        private readonly float deltaTime;

        /// <summary>Creates a move command for one step.</summary>
        /// <param name="worldVelocity">Desired world-space velocity in units per second.</param>
        /// <param name="deltaTime">Length of the step the motor should simulate.</param>
        public MovePawnCommand(Vector3 worldVelocity, float deltaTime)
        {
            this.worldVelocity = worldVelocity;
            this.deltaTime = deltaTime;
        }

        /// <summary>Moves the target's motor, or does nothing when the target has none.</summary>
        /// <param name="provider">The capability provider the controller is driving.</param>
        public void Execute(ICapabilityProvider provider)
        {
            if (provider.TryGet(out IPawnMotor motor))
                motor.Move(worldVelocity, deltaTime);
        }
    }
}
