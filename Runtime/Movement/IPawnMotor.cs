using UnityEngine;

namespace EldritchGames.PawnSystem.Movement
{
    /// <summary>
    /// How a <see cref="Pawn"/> moves through the world. The package defines the verbs; the game
    /// supplies the implementation — a <c>CharacterController</c>, a <c>Rigidbody</c>, a NavMesh
    /// agent, a grid stepper, a 2D controller, or a tween for a turn-based board.
    /// </summary>
    /// <remarks>
    /// Movement is a capability, not a requirement: a turret, a portrait in a turn-based battle or
    /// a security camera is a perfectly good pawn with no motor at all. Reach it through
    /// <see cref="Pawn.Motor"/> or <see cref="Pawn.TryGet{T}"/>.
    /// <code>
    /// if (pawn.TryGet&lt;IPawnMotor&gt;(out var motor))
    ///     motor.Move(inputDirection * speed, Time.deltaTime);
    /// </code>
    /// <para>
    /// <see cref="Move"/> takes a world-space <em>velocity</em>, not an input direction: deciding
    /// how fast the pawn goes belongs to the game's speed and status rules, not to the motor. Time
    /// is passed in rather than read from <c>Time.deltaTime</c> so motors work under a fixed step,
    /// a paused clock, or a test with no frames at all.
    /// </para>
    /// </remarks>
    public interface IPawnMotor
    {
        /// <summary>The pawn's current world-space velocity, including any vertical component the motor manages.</summary>
        Vector3 Velocity { get; }

        /// <summary>
        /// <c>true</c> when the pawn is standing on something. Motors with no notion of ground —
        /// a flyer, a 2D top-down mover, a board piece — should report <c>true</c>.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>Moves the pawn for one step of <paramref name="deltaTime"/>.</summary>
        /// <param name="worldVelocity">Desired world-space velocity in units per second. <see cref="Vector3.zero"/> means "stand still but keep simulating", which is how gravity keeps working.</param>
        /// <param name="deltaTime">Length of this step. Callers pass <c>Time.deltaTime</c>, a fixed step, or a scripted value.</param>
        void Move(Vector3 worldVelocity, float deltaTime);

        /// <summary>Faces the pawn in a direction.</summary>
        /// <param name="rotation">The world-space rotation to adopt.</param>
        /// <remarks>Implementations may ease toward the rotation rather than snapping; do not assume it applies immediately.</remarks>
        void SetLookRotation(Quaternion rotation);

        /// <summary>Places the pawn somewhere immediately, with no interpolation and no collision sweep.</summary>
        /// <param name="position">The world-space position to move to.</param>
        /// <param name="rotation">The world-space rotation to adopt.</param>
        /// <remarks>
        /// Used by <see cref="Pawn.Spawn(Vector3, Quaternion)"/> and by teleport abilities.
        /// Implementations must clear any accumulated velocity so the pawn does not arrive falling.
        /// </remarks>
        void Teleport(Vector3 position, Quaternion rotation);

        /// <summary>Stops the pawn and clears any accumulated velocity.</summary>
        void Stop();
    }
}
