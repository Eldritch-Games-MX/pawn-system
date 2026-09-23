using EldritchGames.AbilitySystem;
using EldritchGames.AbilitySystem.Definitions;
using UnityEngine;

namespace EldritchGames.PawnSystem.AbilitySystem
{
    /// <summary>
    /// Wires a pawn's vitals to an ability-system attribute from the inspector: add the component,
    /// pick the attribute, done.
    /// </summary>
    /// <remarks>
    /// Equivalent to calling <see cref="Pawn.SetVitalSource"/> with an
    /// <see cref="AttributeVitalSource"/> in <c>Awake</c>, but without writing a script per project.
    /// <para>
    /// Runs late in <c>Awake</c> (execution order 100) so the <see cref="AbilitySystemComponent"/>
    /// has already registered its initial attributes, and still before any <c>Start</c>, so the
    /// pawn spawns with the bound vitals rather than a throwaway <c>Health</c>.
    /// </para>
    /// <para>
    /// The pawn's <c>Max Vital</c> field is ignored once bound — the attribute asset, or the max
    /// attribute when one is given, owns the maximum.
    /// </para>
    /// </remarks>
    [DefaultExecutionOrder(100)]
    public sealed class AbilityVitalsBinder : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("The pawn whose vitals to bind. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private Pawn pawn;

        [Tooltip("The ability system component holding the attribute. Leave empty to find it on this GameObject or a parent.")]
        [SerializeField] private AbilitySystemComponent abilitySystem;

        [Header("Configuration")]
        [Tooltip("The attribute that holds the pawn's current vitals — health, hull, sanity.")]
        [SerializeField] private AttributeDefinition vitalAttribute;

        [Tooltip("Optional attribute holding the maximum, for games where buffs raise max health. Leave empty to use the attribute's own Max Value.")]
        [SerializeField] private AttributeDefinition maxVitalAttribute;

        private AttributeVitalSource source;

        /// <summary>The vital source this binder created, or <c>null</c> before <c>Awake</c> or after a failed bind.</summary>
        public AttributeVitalSource Source => source;

        private void Awake()
        {
            if (pawn == null) pawn = GetComponentInParent<Pawn>();
            if (abilitySystem == null) abilitySystem = GetComponentInParent<AbilitySystemComponent>();

            if (pawn == null || abilitySystem == null || vitalAttribute == null)
            {
                Debug.LogError(
                    $"AbilityVitalsBinder on '{name}' needs a Pawn, an AbilitySystemComponent and a vital attribute. The pawn keeps its own Health until all three are assigned.",
                    this);
                return;
            }

            source = new AttributeVitalSource(abilitySystem, vitalAttribute, maxVitalAttribute);
            pawn.SetVitalSource(source);
        }

        private void OnDestroy()
        {
            source?.Dispose();
            source = null;
        }
    }
}
