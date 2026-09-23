using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// An authored secondary resource — mana, stamina, ammunition, rage. One asset per resource,
    /// shared by every pawn that has it.
    /// </summary>
    /// <remarks>
    /// Where <see cref="Identity.PawnDefinition.MaxVital"/> is the single number a pawn dies when
    /// it runs out of, a <c>ResourceDefinition</c> is one of any number of <em>other</em> pools a
    /// pawn can spend from — nothing about running out of one kills the pawn.
    /// <para>
    /// Create with <b>Assets &gt; Create &gt; Eldritch Games &gt; Pawn System &gt; Resource
    /// Definition</b>, then list it in <see cref="Identity.PawnDefinition.InitialResources"/> so
    /// pawns of that archetype start with it registered and full.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Eldritch Games/Pawn System/Resource Definition", fileName = "NewResourceDefinition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used by save data to resolve this asset again. Never shown to players.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Human-readable name for UI and debugging.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Icon for UI. Authoring metadata only — no runtime code reads it.")]
        [SerializeField] private Sprite icon;

        [Tooltip("The maximum, and starting, value a pawn registers this resource with.")]
        [SerializeField] private float defaultMax = 100f;

        /// <summary>Stable identifier used by save data to resolve this asset. Never shown to players.</summary>
        public string Id => id;

        /// <summary>Human-readable name for UI and debugging.</summary>
        public string DisplayName => displayName;

        /// <summary>Icon for UI. Authoring metadata — no runtime code in this package reads it.</summary>
        public Sprite Icon => icon;

        /// <summary>The maximum, and starting, value a pawn registers this resource with. Must be greater than zero.</summary>
        public float DefaultMax => defaultMax;
    }
}
