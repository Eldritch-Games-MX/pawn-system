using EldritchGames.PawnSystem.Movement;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.Animation
{
    /// <summary>
    /// Wires a <see cref="Pawn"/>'s lifecycle and motion onto an <see cref="Animator"/>, without
    /// either one knowing about the other. Add it beside a <c>Pawn</c> and an <c>Animator</c>,
    /// name the parameters that exist on your controller, leave the rest blank.
    /// </summary>
    /// <remarks>
    /// Genre-agnostic the way the rest of the package is: this component does not assume a
    /// blend tree, a humanoid rig, or even that every parameter exists — an empty parameter name
    /// simply skips that binding, and a parameter Unity's Animator does not recognise no-ops
    /// silently rather than logging.
    /// <code>
    /// // On the pawn's GameObject, alongside Pawn and Animator:
    /// //   Speed Parameter    = "Speed"
    /// //   Grounded Parameter = "IsGrounded"
    /// //   Died Trigger       = "Died"
    /// //   Revived Trigger    = "Revived"
    /// //   Hit Trigger        = "" (this controller has no hit reaction — left blank, skipped)
    /// </code>
    /// <para>
    /// This is the one type in the package with an <c>Update</c>. Everything else is driven by
    /// <see cref="Pawn.Tick"/> because simulation time is the host's to control — but Animator
    /// parameter smoothing is a presentation concern tied to the render frame, not the
    /// simulation clock, so it is the deliberate, scoped exception.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorPawnBridge : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("The pawn to reflect. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private Pawn pawn;

        [Tooltip("The Animator to drive. Leave empty to find it on this GameObject.")]
        [SerializeField] private Animator animator;

        [Header("Continuous parameters")]
        [Tooltip("Float parameter set every frame to the motor's horizontal speed. Leave blank to skip.")]
        [SerializeField] private string speedParameter = "Speed";

        [Tooltip("Bool parameter set every frame to the motor's grounded state. Leave blank to skip.")]
        [SerializeField] private string groundedParameter = "IsGrounded";

        [Header("Event triggers")]
        [Tooltip("Trigger fired when the pawn dies. Leave blank to skip.")]
        [SerializeField] private string diedTrigger = "Died";

        [Tooltip("Trigger fired when the pawn is downed but not dead. Leave blank to skip.")]
        [SerializeField] private string incapacitatedTrigger = "Incapacitated";

        [Tooltip("Trigger fired when the pawn recovers from death or incapacitation. Leave blank to skip.")]
        [SerializeField] private string revivedTrigger = "Revived";

        [Tooltip("Trigger fired on every hit that reaches the pawn, including ones fully absorbed. Leave blank to skip.")]
        [SerializeField] private string hitTrigger;

        /// <summary>The pawn this bridge reflects.</summary>
        public Pawn Pawn => pawn;

        /// <summary>The Animator this bridge drives.</summary>
        public Animator TargetAnimator => animator;

        private void Awake()
        {
            if (pawn == null) pawn = GetComponentInParent<Pawn>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (pawn == null) return;

            pawn.Died += OnDied;
            pawn.Incapacitated += OnIncapacitated;
            pawn.Revived += OnRevived;
            if (!string.IsNullOrEmpty(hitTrigger)) pawn.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            if (pawn == null) return;

            pawn.Died -= OnDied;
            pawn.Incapacitated -= OnIncapacitated;
            pawn.Revived -= OnRevived;
            pawn.DamageTaken -= OnDamageTaken;
        }

        private void Update()
        {
            if (pawn == null || animator == null) return;
            if (animator.runtimeAnimatorController == null) return;
            if (!pawn.TryGet<IPawnMotor>(out IPawnMotor motor)) return;

            if (!string.IsNullOrEmpty(speedParameter))
            {
                Vector3 velocity = motor.Velocity;
                float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
                animator.SetFloat(speedParameter, horizontalSpeed);
            }

            if (!string.IsNullOrEmpty(groundedParameter)) animator.SetBool(groundedParameter, motor.IsGrounded);
        }

        private void OnDied(Pawn deadPawn, DeathInfo info) => SetTrigger(diedTrigger);

        private void OnIncapacitated(Pawn downedPawn, DeathInfo info) => SetTrigger(incapacitatedTrigger);

        private void OnRevived(Pawn revivedPawn) => SetTrigger(revivedTrigger);

        private void OnDamageTaken(Pawn hitPawn, DamageInfo info) => SetTrigger(hitTrigger);

        private void SetTrigger(string parameter)
        {
            if (animator == null || string.IsNullOrEmpty(parameter)) return;
            if (animator.runtimeAnimatorController == null) return;

            animator.SetTrigger(parameter);
        }
    }
}
