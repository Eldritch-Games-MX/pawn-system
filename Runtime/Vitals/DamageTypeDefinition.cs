using UnityEngine;

namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// An authored kind of damage — physical, fire, psychic, falling, scripted. One asset per kind.
    /// </summary>
    /// <remarks>
    /// Damage types are assets rather than an enum so a project can add a kind without editing
    /// this package, and so resistances can reference the type directly instead of matching on a
    /// number. A <c>null</c> damage type is legal everywhere and means "unspecified".
    /// <para>
    /// <see cref="IgnoresInvulnerability"/> replaces the hard-coded "true damage" case of the older
    /// pawn code: mark a scripted execution, a pit of spikes or a story death this way and it lands
    /// even while <see cref="Pawn.IsInvulnerable"/> is set.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Eldritch Games/Pawn System/Damage Type Definition", fileName = "NewDamageTypeDefinition")]
    public sealed class DamageTypeDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used by save data and by resistance lookups. Never shown to players.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Human-readable name for UI, floating combat text and debugging.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("When set, damage of this type lands even while the pawn is invulnerable. Use for scripted or story deaths.")]
        [SerializeField] private bool ignoresInvulnerability;

        /// <summary>Stable identifier used by save data and resistance lookups. Never shown to players.</summary>
        public string Id => id;

        /// <summary>Human-readable name for UI and debugging.</summary>
        public string DisplayName => displayName;

        /// <summary>
        /// When <c>true</c>, damage of this type bypasses <see cref="Pawn.IsInvulnerable"/>.
        /// Invulnerability still never protects against <see cref="Pawn.Kill"/>.
        /// </summary>
        public bool IgnoresInvulnerability => ignoresInvulnerability;
    }
}
