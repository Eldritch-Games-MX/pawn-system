using System.Collections.Generic;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// The authored recipe for a kind of pawn: who it is, which side it is on, how much punishment
    /// it takes, and how it behaves when it dies. One asset per archetype — <c>Knight</c>,
    /// <c>Rat</c>, <c>SecurityCamera</c> — shared by every instance of that archetype.
    /// </summary>
    /// <remarks>
    /// Authored data only. Runtime state lives on <see cref="Pawn"/>, which reads this asset when
    /// it spawns and never writes to it. Two pawns sharing a definition share no state.
    /// <para>
    /// Create with <b>Assets &gt; Create &gt; Eldritch Games &gt; Pawn System &gt; Pawn Definition</b>.
    /// </para>
    /// <para>
    /// <see cref="Icon"/> and <see cref="DisplayName"/> are presentation only — no runtime code in
    /// this package reads them. <see cref="Id"/> is the opposite: it is never shown to players and
    /// exists so save data can find this asset again.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Eldritch Games/Pawn System/Pawn Definition", fileName = "NewPawnDefinition")]
    public sealed class PawnDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier used by save data to find this asset again. Never shown to players.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Human-readable name for UI, kill feeds and debugging.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Portrait or marker for UI. Authoring metadata only — no runtime code reads it.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Side this pawn starts on. A pawn can change teams at runtime.")]
        [SerializeField] private TeamDefinition team;

        [Tooltip("Dotted tags every pawn of this kind starts with, for example Class.Rogue or Faction.Undead.")]
        [SerializeField] private List<PawnTag> tags = new List<PawnTag>();

        [Header("Vitals")]
        [Tooltip("Maximum health, hull, sanity — whatever this pawn dies when it runs out of.")]
        [SerializeField] private float maxVital = 100f;

        [Tooltip("Damage pipeline applied before anything reaches the pawn's vitals, in order, top to bottom.")]
        [SerializeReference, SerializeReferenceDropdown]
        private List<IDamageModifier> damageModifiers = new List<IDamageModifier>();

        [Header("Lifecycle")]
        [Tooltip("Seconds of invulnerability granted after spawning and after reviving. Zero disables the grace period.")]
        [SerializeField] private float spawnInvulnerability;

        [Tooltip("Release the possessor automatically when this pawn dies. Turn off for games where a player keeps driving a corpse.")]
        [SerializeField] private bool releaseOnDeath = true;

        [Tooltip("Deactivate the GameObject when this pawn despawns. Turn off when something else handles the visuals.")]
        [SerializeField] private bool deactivateOnDespawn = true;

        /// <summary>Stable identifier used by save data to resolve this asset. Never shown to players.</summary>
        public string Id => id;

        /// <summary>Human-readable name for UI, kill feeds and debugging.</summary>
        public string DisplayName => displayName;

        /// <summary>Portrait or marker for UI. Authoring metadata — no runtime code in this package reads it.</summary>
        public Sprite Icon => icon;

        /// <summary>The side a pawn of this kind starts on, or <c>null</c> for no team.</summary>
        public TeamDefinition Team => team;

        /// <summary>Tags every pawn of this kind starts with. Copied into <see cref="Pawn.Tags"/> on spawn.</summary>
        public IReadOnlyList<PawnTag> Tags => tags;

        /// <summary>The maximum value of the pawn's vitals. Must be greater than zero.</summary>
        public float MaxVital => maxVital;

        /// <summary>
        /// The damage pipeline for this archetype, applied in order before any component-based
        /// modifiers on the pawn itself.
        /// </summary>
        public IReadOnlyList<IDamageModifier> DamageModifiers => damageModifiers;

        /// <summary>Seconds of invulnerability granted after spawning and after reviving. Zero disables it.</summary>
        public float SpawnInvulnerability => spawnInvulnerability;

        /// <summary>
        /// Whether death automatically releases the possessor. <c>true</c> matches the usual
        /// convention that a controller does not drive a corpse.
        /// </summary>
        public bool ReleaseOnDeath => releaseOnDeath;

        /// <summary>Whether <see cref="Pawn.Despawn"/> deactivates the GameObject.</summary>
        public bool DeactivateOnDespawn => deactivateOnDespawn;
    }
}
