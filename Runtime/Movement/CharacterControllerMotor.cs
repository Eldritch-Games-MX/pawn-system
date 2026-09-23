using UnityEngine;

namespace EldritchGames.PawnSystem.Movement
{
    /// <summary>
    /// The built-in <see cref="IPawnMotor"/>: drives a Unity <see cref="CharacterController"/> with
    /// simple gravity and optional smoothed turning. Enough for a prototype of most 3D genres, and
    /// the worked example to copy when writing a motor of your own.
    /// </summary>
    /// <remarks>
    /// Put it on the same GameObject as the <see cref="CharacterController"/>; the pawn finds it
    /// through <see cref="Pawn.Motor"/>.
    /// <code>
    /// if (pawn.TryGet&lt;IPawnMotor&gt;(out var motor))
    ///     motor.Move(new Vector3(stick.x, 0f, stick.y) * moveSpeed, Time.deltaTime);
    /// </code>
    /// Gravity accumulates while airborne and is zeroed on landing. Horizontal velocity is whatever
    /// the caller asked for — this motor deliberately applies no acceleration, friction or air
    /// control, because those are genre decisions.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerMotor : MonoBehaviour, IPawnMotor
    {
        [Header("Configuration")]
        [Tooltip("Downward acceleration in units per second squared. Zero disables gravity for flyers and top-down games.")]
        [SerializeField] private float gravity = -9.81f;

        [Tooltip("Degrees per second the pawn turns toward a requested rotation. Zero snaps instantly.")]
        [SerializeField] private float turnSpeed;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private Quaternion targetRotation;

        /// <inheritdoc/>
        public Vector3 Velocity => new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

        /// <inheritdoc/>
        public bool IsGrounded => controller != null && controller.isGrounded;

        /// <inheritdoc/>
        /// <remarks>
        /// Applies <paramref name="worldVelocity"/> horizontally, accumulates gravity vertically,
        /// and eases toward the last requested look rotation at <c>Turn Speed</c>.
        /// </remarks>
        public void Move(Vector3 worldVelocity, float deltaTime)
        {
            if (controller == null || deltaTime <= 0f) return;

            if (IsGrounded && verticalVelocity < 0f) verticalVelocity = 0f;
            else verticalVelocity += gravity * deltaTime;

            horizontalVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);

            Vector3 step = (horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime;
            controller.Move(step);

            if (turnSpeed > 0f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        /// <inheritdoc/>
        /// <remarks>Snaps when <c>Turn Speed</c> is zero; otherwise eases toward the rotation on the next <see cref="Move"/>.</remarks>
        public void SetLookRotation(Quaternion rotation)
        {
            targetRotation = rotation;
            if (turnSpeed <= 0f) transform.rotation = rotation;
        }

        /// <inheritdoc/>
        /// <remarks>Disables the controller for the move so it does not fight the transform, then clears all velocity.</remarks>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Stop();
            targetRotation = rotation;

            if (controller == null)
            {
                transform.SetPositionAndRotation(position, rotation);
                return;
            }

            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = wasEnabled;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            targetRotation = transform.rotation;
        }
    }
}
