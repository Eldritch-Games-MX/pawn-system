using EldritchGames.InteractionSystem;
using UnityEngine;

namespace EldritchGames.PawnSystem.InteractionSystem
{
    /// <summary>
    /// Makes a <see cref="Pawn"/> discoverable by the Eldritch Interaction System — an
    /// <see cref="IInteractable"/> that forwards capability queries to the pawn's own
    /// <see cref="Pawn.TryGet{T}"/>.
    /// </summary>
    /// <remarks>
    /// Add this component to a pawn and it becomes a valid target for a
    /// <c>ProximityInteractionZone</c>. It declares no capabilities of its own — a game adds
    /// <c>IDialogueTarget</c>, <c>IActivatable</c>, <c>ICollectible</c> or its own interfaces as
    /// separate components, exactly as it would on a plain <c>Interactable</c>, and this bridge
    /// finds them through the pawn the same way <c>PawnInputTarget</c> finds capabilities for
    /// commands.
    /// <code>
    /// public sealed class LootableCorpse : MonoBehaviour, ICollectible
    /// {
    ///     [SerializeField] private Pawn pawn;
    ///     public bool TryCollect() => pawn.State == PawnState.Dead;   // only lootable once dead
    /// }
    ///
    /// // on the same GameObject as Pawn:
    /// //   PawnInteractable
    /// //   LootableCorpse
    /// </code>
    /// <para>
    /// A capability that should only apply in certain states — a corpse being lootable, a living
    /// NPC being a dialogue target — reads <see cref="Pawn.State"/> itself; this bridge does not
    /// gate anything, matching the rest of the package's stay-out-of-policy approach.
    /// </para>
    /// </remarks>
    public sealed class PawnInteractable : MonoBehaviour, IInteractable
    {
        [Header("Bindings")]
        [Tooltip("The pawn this interactable represents. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private Pawn pawn;

        /// <summary>The pawn this interactable represents.</summary>
        public Pawn Pawn
        {
            get => pawn;
            set => pawn = value;
        }

        /// <inheritdoc/>
        public Transform Transform => transform;

        /// <summary>Forwards a capability query to the pawn.</summary>
        /// <typeparam name="T">The capability interface an interaction command is asking for.</typeparam>
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
