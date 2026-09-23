using EldritchGames.InputSystem;
using EldritchGames.PawnSystem.Movement;
using UnityEngine;

namespace EldritchGames.PawnSystem.InputSystem
{
    /// <summary>
    /// Turns whatever pawn it is executed against, by asking for an <see cref="IPawnMotor"/> and
    /// doing nothing if there is not one.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="MovePawnCommand"/> because plenty of games aim and move
    /// independently — twin-stick, mouse-look, a turret tracking while the chassis drives.
    /// <code>
    /// buffer.Add(LookPawnCommand.Towards(aimDirection));
    /// </code>
    /// Whether the pawn snaps or eases to the rotation is the motor's business.
    /// </remarks>
    public sealed class LookPawnCommand : ICommand
    {
        private readonly Quaternion rotation;

        /// <summary>Creates a look command for an exact rotation.</summary>
        /// <param name="rotation">The world-space rotation the pawn should adopt.</param>
        public LookPawnCommand(Quaternion rotation)
        {
            this.rotation = rotation;
        }

        /// <summary>Creates a look command from a world-space direction.</summary>
        /// <param name="direction">Which way to face. A zero direction produces a command that does nothing.</param>
        /// <returns>A command that turns the pawn toward <paramref name="direction"/>.</returns>
        public static LookPawnCommand Towards(Vector3 direction) =>
            new LookPawnCommand(direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction) : Quaternion.identity);

        /// <summary>Turns the target's motor, or does nothing when the target has none.</summary>
        /// <param name="provider">The capability provider the controller is driving.</param>
        public void Execute(ICapabilityProvider provider)
        {
            if (provider.TryGet(out IPawnMotor motor))
                motor.SetLookRotation(rotation);
        }
    }
}
