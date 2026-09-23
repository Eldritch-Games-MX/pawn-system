using System.Collections.Generic;
using System.Reflection;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// Builds <see cref="PawnDefinition"/> assets in memory for tests, setting the private
    /// serialized fields directly so the runtime type needs no test-only setters.
    /// </summary>
    /// <remarks>
    /// <code>
    /// PawnDefinition definition = new PawnDefinitionBuilder()
    ///     .WithId("knight")
    ///     .WithMaxVital(50f)
    ///     .WithDamageModifiers(new FlatDamageReduction())
    ///     .Build();
    /// </code>
    /// The instance is a <c>ScriptableObject.CreateInstance</c>, never an asset on disk, so nothing
    /// needs cleaning up beyond the GameObjects a test creates itself.
    /// </remarks>
    public sealed class PawnDefinitionBuilder
    {
        private readonly PawnDefinition definition = ScriptableObject.CreateInstance<PawnDefinition>();

        /// <summary>Sets the save-data identifier.</summary>
        /// <param name="id">The identifier.</param>
        public PawnDefinitionBuilder WithId(string id) => SetField("id", id);

        /// <summary>Sets the human-readable name.</summary>
        /// <param name="displayName">The display name.</param>
        public PawnDefinitionBuilder WithDisplayName(string displayName) => SetField("displayName", displayName);

        /// <summary>Sets the starting team.</summary>
        /// <param name="team">The team asset.</param>
        public PawnDefinitionBuilder WithTeam(TeamDefinition team) => SetField("team", team);

        /// <summary>Sets the tags pawns of this kind start with.</summary>
        /// <param name="tags">The tags.</param>
        public PawnDefinitionBuilder WithTags(params PawnTag[] tags) => SetField("tags", new List<PawnTag>(tags));

        /// <summary>Sets the maximum vital value.</summary>
        /// <param name="maxVital">The maximum.</param>
        public PawnDefinitionBuilder WithMaxVital(float maxVital) => SetField("maxVital", maxVital);

        /// <summary>Sets the authored damage pipeline, in order.</summary>
        /// <param name="modifiers">The modifiers.</param>
        public PawnDefinitionBuilder WithDamageModifiers(params IDamageModifier[] modifiers) =>
            SetField("damageModifiers", new List<IDamageModifier>(modifiers));

        /// <summary>Sets the grace period granted on spawn and revival.</summary>
        /// <param name="seconds">The duration in ticked time.</param>
        public PawnDefinitionBuilder WithSpawnInvulnerability(float seconds) => SetField("spawnInvulnerability", seconds);

        /// <summary>Sets whether death releases the possessor.</summary>
        /// <param name="releaseOnDeath">Whether to release.</param>
        public PawnDefinitionBuilder WithReleaseOnDeath(bool releaseOnDeath) => SetField("releaseOnDeath", releaseOnDeath);

        /// <summary>Sets whether despawning deactivates the GameObject.</summary>
        /// <param name="deactivateOnDespawn">Whether to deactivate.</param>
        public PawnDefinitionBuilder WithDeactivateOnDespawn(bool deactivateOnDespawn) =>
            SetField("deactivateOnDespawn", deactivateOnDespawn);

        /// <summary>Sets whether a fatal blow downs this pawn instead of killing it outright.</summary>
        /// <param name="canBeIncapacitated">Whether to allow incapacitation.</param>
        public PawnDefinitionBuilder WithCanBeIncapacitated(bool canBeIncapacitated) =>
            SetField("canBeIncapacitated", canBeIncapacitated);

        /// <summary>Sets the bleed-out timer for a downed pawn.</summary>
        /// <param name="seconds">Seconds until an unrecovered pawn dies. Zero or less disables the timer.</param>
        public PawnDefinitionBuilder WithIncapacitationDuration(float seconds) =>
            SetField("incapacitationDuration", seconds);

        /// <summary>Sets whether being downed releases the possessor.</summary>
        /// <param name="releaseOnIncapacitation">Whether to release.</param>
        public PawnDefinitionBuilder WithReleaseOnIncapacitation(bool releaseOnIncapacitation) =>
            SetField("releaseOnIncapacitation", releaseOnIncapacitation);

        /// <summary>Sets the secondary resources this pawn starts with.</summary>
        /// <param name="resources">The resources.</param>
        public PawnDefinitionBuilder WithInitialResources(params ResourceDefinition[] resources) =>
            SetField("initialResources", new List<ResourceDefinition>(resources));

        /// <summary>Returns the configured definition.</summary>
        public PawnDefinition Build() => definition;

        private PawnDefinitionBuilder SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(PawnDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, value);
            return this;
        }
    }
}
