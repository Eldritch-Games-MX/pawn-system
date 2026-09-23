using EldritchGames.PawnSystem.Movement;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// An <see cref="IPawnMotor"/> spy: records what it was asked to do without touching physics,
    /// so movement plumbing can be tested in EditMode with no scene.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var motor = new FakePawnMotor();
    /// pawn.SetMotor(motor);
    /// new MovePawnCommand(Vector3.forward * 3f, 0.5f).Execute(inputTarget);
    /// Assert.AreEqual(Vector3.forward * 3f, motor.LastVelocity, "The command must forward the velocity it was built with.");
    /// </code>
    /// </remarks>
    public sealed class FakePawnMotor : IPawnMotor
    {
        /// <inheritdoc/>
        public Vector3 Velocity { get; private set; }

        /// <inheritdoc/>
        /// <remarks>Settable, so a test can pretend the pawn is airborne.</remarks>
        public bool IsGrounded { get; set; } = true;

        /// <summary>How many times <see cref="Move"/> has been called.</summary>
        public int MoveCallCount { get; private set; }

        /// <summary>The velocity passed to the most recent <see cref="Move"/>.</summary>
        public Vector3 LastVelocity { get; private set; }

        /// <summary>The step length passed to the most recent <see cref="Move"/>.</summary>
        public float LastDeltaTime { get; private set; }

        /// <summary>The rotation passed to the most recent <see cref="SetLookRotation"/>.</summary>
        public Quaternion LastLookRotation { get; private set; } = Quaternion.identity;

        /// <summary>How many times <see cref="Teleport"/> has been called.</summary>
        public int TeleportCallCount { get; private set; }

        /// <summary>The position passed to the most recent <see cref="Teleport"/>.</summary>
        public Vector3 LastTeleportPosition { get; private set; }

        /// <summary>How many times <see cref="Stop"/> has been called.</summary>
        public int StopCallCount { get; private set; }

        /// <inheritdoc/>
        public void Move(Vector3 worldVelocity, float deltaTime)
        {
            MoveCallCount++;
            LastVelocity = worldVelocity;
            LastDeltaTime = deltaTime;
            Velocity = worldVelocity;
        }

        /// <inheritdoc/>
        public void SetLookRotation(Quaternion rotation) => LastLookRotation = rotation;

        /// <inheritdoc/>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            TeleportCallCount++;
            LastTeleportPosition = position;
            LastLookRotation = rotation;
            Velocity = Vector3.zero;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            StopCallCount++;
            Velocity = Vector3.zero;
        }
    }
}
