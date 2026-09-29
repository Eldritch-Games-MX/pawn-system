using System.Collections.Generic;
using System.Reflection;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// Builds <see cref="ActorDefinition"/> assets in memory for tests, setting the private
    /// serialized fields directly so the runtime type needs no test-only setters.
    /// </summary>
    /// <remarks>
    /// <code>
    /// ActorDefinition definition = new ActorDefinitionBuilder()
    ///     .WithId("crate")
    ///     .WithMaxVital(20f)
    ///     .Build();
    /// </code>
    /// The instance is a <c>ScriptableObject.CreateInstance</c>, never an asset on disk, so nothing
    /// needs cleaning up beyond the GameObjects a test creates itself. See
    /// <see cref="PawnDefinitionBuilder"/> for the possessable counterpart.
    /// </remarks>
    public sealed class ActorDefinitionBuilder
    {
        private readonly ActorDefinition definition = ScriptableObject.CreateInstance<ActorDefinition>();

        /// <summary>Sets the save-data identifier.</summary>
        /// <param name="id">The identifier.</param>
        public ActorDefinitionBuilder WithId(string id) => SetField("id", id);

        /// <summary>Sets the human-readable name.</summary>
        /// <param name="displayName">The display name.</param>
        public ActorDefinitionBuilder WithDisplayName(string displayName) => SetField("displayName", displayName);

        /// <summary>Sets the tags actors of this kind start with.</summary>
        /// <param name="tags">The tags.</param>
        public ActorDefinitionBuilder WithTags(params PawnTag[] tags) => SetField("tags", new List<PawnTag>(tags));

        /// <summary>Sets the maximum vital value.</summary>
        /// <param name="maxVital">The maximum.</param>
        public ActorDefinitionBuilder WithMaxVital(float maxVital) => SetField("maxVital", maxVital);

        /// <summary>Sets the authored damage pipeline, in order.</summary>
        /// <param name="modifiers">The modifiers.</param>
        public ActorDefinitionBuilder WithDamageModifiers(params IDamageModifier[] modifiers) =>
            SetField("damageModifiers", new List<IDamageModifier>(modifiers));

        /// <summary>Sets the grace period granted on spawn and revival.</summary>
        /// <param name="seconds">The duration in ticked time.</param>
        public ActorDefinitionBuilder WithSpawnInvulnerability(float seconds) => SetField("spawnInvulnerability", seconds);

        /// <summary>Sets whether despawning deactivates the GameObject.</summary>
        /// <param name="deactivateOnDespawn">Whether to deactivate.</param>
        public ActorDefinitionBuilder WithDeactivateOnDespawn(bool deactivateOnDespawn) =>
            SetField("deactivateOnDespawn", deactivateOnDespawn);

        /// <summary>Sets whether a fatal blow downs this actor instead of destroying it outright.</summary>
        /// <param name="canBeIncapacitated">Whether to allow incapacitation.</param>
        public ActorDefinitionBuilder WithCanBeIncapacitated(bool canBeIncapacitated) =>
            SetField("canBeIncapacitated", canBeIncapacitated);

        /// <summary>Sets the collapse timer for a downed actor.</summary>
        /// <param name="seconds">Seconds until an unrecovered actor is destroyed. Zero or less disables the timer.</param>
        public ActorDefinitionBuilder WithIncapacitationDuration(float seconds) =>
            SetField("incapacitationDuration", seconds);

        /// <summary>Returns the configured definition.</summary>
        public ActorDefinition Build() => definition;

        private ActorDefinitionBuilder SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(ActorDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, value);
            return this;
        }
    }
}
