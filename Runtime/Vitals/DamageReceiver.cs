using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// A hit zone. Put one on each collider that should hurt a <see cref="Pawn"/> — head, torso,
    /// a mech's weak point — and it forwards damage to the pawn, scaled and tagged with which
    /// zone was struck.
    /// </summary>
    /// <remarks>
    /// The pawn itself is already <see cref="IDamageable"/>, so a single-collider pawn needs no
    /// receiver at all. Add receivers only when different parts of the body should behave
    /// differently.
    /// <code>
    /// // On the head collider: multiplier 2, body part "Body.Head"
    /// // A weapon hits the collider and does not care which it found:
    /// if (hit.collider.TryGetComponent&lt;IDamageable&gt;(out var target))
    ///     target.ApplyDamage(new DamageInfo(25f, instigator: shooter));  // 50 to the pawn
    /// </code>
    /// The owning pawn is found on this GameObject or any parent when it is not assigned in the
    /// inspector, so dropping the component onto a bone in a rig just works.
    /// </remarks>
    public sealed class DamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Bindings")]
        [Tooltip("The pawn this hit zone belongs to. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private Pawn pawn;

        [Header("Configuration")]
        [Tooltip("Dotted tag naming this hit zone, for example Body.Head. Travels with the damage and the death record.")]
        [SerializeField] private PawnTag bodyPart;

        [Tooltip("Incoming damage is multiplied by this before it reaches the pawn.")]
        [SerializeField] private float damageMultiplier = 1f;

        /// <summary>The pawn this hit zone forwards damage to, or <c>null</c> when none was found.</summary>
        public Pawn Pawn => pawn;

        /// <summary>The dotted tag naming this hit zone, such as <c>"Body.Head"</c>.</summary>
        public PawnTag BodyPart => bodyPart;

        /// <summary>The multiplier applied to incoming damage before it reaches the pawn.</summary>
        public float DamageMultiplier => damageMultiplier;

        /// <summary>
        /// Scales <paramref name="damage"/> by <see cref="DamageMultiplier"/>, stamps it with
        /// <see cref="BodyPart"/>, and forwards it to the owning pawn.
        /// </summary>
        /// <param name="damage">The incoming damage event.</param>
        /// <returns>
        /// How much the pawn actually took, or zero when this receiver has no pawn — an orphaned
        /// hit zone absorbs damage silently rather than throwing.
        /// </returns>
        public float ApplyDamage(in DamageInfo damage)
        {
            if (pawn == null) return 0f;

            DamageInfo scaled = damage
                .WithAmount(damage.Amount * damageMultiplier)
                .WithBodyPart(bodyPart);

            return pawn.ApplyDamage(scaled);
        }

        private void Awake()
        {
            if (pawn == null) pawn = GetComponentInParent<Pawn>();
        }
    }
}
