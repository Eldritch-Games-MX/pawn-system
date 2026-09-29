using System.Collections.Generic;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// The authored recipe for a kind of <see cref="Actor"/>: who it is, how much punishment it
    /// takes, and how it behaves when it dies. One asset per archetype — <c>WoodenCrate</c>,
    /// <c>ExplosiveBarrel</c>, <c>SecurityCamera</c> — shared by every instance of that archetype.
    /// </summary>
    /// <remarks>
    /// The <see cref="Identity.PawnDefinition"/> of a world object that is never possessed: no team,
    /// no possession-release flags, no secondary resources. If an archetype ever needs those, it
    /// needs a <see cref="Pawn"/>, not an <see cref="Actor"/> — see <c>Documentation~/pawn-taxonomy.md</c>.
    /// <para>
    /// Create with <b>Assets &gt; Create &gt; Eldritch Games &gt; Pawn System &gt; Actor Definition</b>.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Eldritch Games/Pawn System/Actor Definition", fileName = "NewActorDefinition")]
    public sealed class ActorDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier used by save data to find this asset again. Never shown to players.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Human-readable name for UI and debugging.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Portrait or marker for UI. Authoring metadata only — no runtime code reads it.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Dotted tags every actor of this kind starts with, for example Class.Explosive or Material.Wood.")]
        [SerializeField] private List<PawnTag> tags = new List<PawnTag>();

        [Header("Vitals")]
        [Tooltip("Maximum health, structural integrity — whatever this actor is destroyed when it runs out of.")]
        [SerializeField] private float maxVital = 100f;

        [Tooltip("Damage pipeline applied before anything reaches the actor's vitals, in order, top to bottom.")]
        [SerializeReference, SerializeReferenceDropdown]
        private List<IDamageModifier> damageModifiers = new List<IDamageModifier>();

        [Header("Lifecycle")]
        [Tooltip("Seconds of invulnerability granted after spawning and after reviving. Zero disables the grace period.")]
        [SerializeField] private float spawnInvulnerability;

        [Tooltip("Deactivate the GameObject when this actor despawns. Turn off when something else handles the visuals.")]
        [SerializeField] private bool deactivateOnDespawn = true;

        [Header("Incapacitation")]
        [Tooltip("Allow this actor to be knocked into a broken-but-not-gone state instead of destroyed outright by a fatal blow. Off by default.")]
        [SerializeField] private bool canBeIncapacitated;

        [Tooltip("Seconds a downed actor survives before it collapses and is destroyed on its own. Zero or less disables the timer.")]
        [SerializeField] private float incapacitationDuration;

        /// <summary>Stable identifier used by save data to resolve this asset. Never shown to players.</summary>
        public string Id => id;

        /// <summary>Human-readable name for UI and debugging.</summary>
        public string DisplayName => displayName;

        /// <summary>Portrait or marker for UI. Authoring metadata — no runtime code in this package reads it.</summary>
        public Sprite Icon => icon;

        /// <summary>Tags every actor of this kind starts with. Copied into <see cref="Actor.Tags"/> on spawn.</summary>
        public IReadOnlyList<PawnTag> Tags => tags;

        /// <summary>The maximum value of the actor's vitals. Must be greater than zero.</summary>
        public float MaxVital => maxVital;

        /// <summary>
        /// The damage pipeline for this archetype, applied in order before any component-based
        /// modifiers on the actor itself.
        /// </summary>
        public IReadOnlyList<IDamageModifier> DamageModifiers => damageModifiers;

        /// <summary>Seconds of invulnerability granted after spawning and after reviving. Zero disables it.</summary>
        public float SpawnInvulnerability => spawnInvulnerability;

        /// <summary>Whether <see cref="Actor.Despawn"/> deactivates the GameObject.</summary>
        public bool DeactivateOnDespawn => deactivateOnDespawn;

        /// <summary>Whether a fatal blow downs this actor (<see cref="PawnState.Incapacitated"/>) instead of destroying it outright.</summary>
        public bool CanBeIncapacitated => canBeIncapacitated;

        /// <summary>Seconds a downed actor survives before collapsing. Zero or less disables the timer.</summary>
        public float IncapacitationDuration => incapacitationDuration;
    }
}
