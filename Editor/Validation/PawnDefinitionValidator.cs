using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using UnityEditor;

namespace EldritchGames.PawnSystem.Editor
{
    /// <summary>
    /// Checks a <see cref="PawnDefinition"/> for the mistakes that only show up at runtime — an
    /// empty id that breaks saves, a modifier whose class was renamed away, vitals that cannot
    /// sustain a living pawn.
    /// </summary>
    /// <remarks>
    /// Returns issues; never logs and never throws, so the same method serves the inline inspector
    /// panel and the project-wide <see cref="PawnDefinitionValidatorWindow"/>.
    /// <code>
    /// foreach (var issue in PawnDefinitionValidator.Validate(definition))
    ///     Debug.LogWarning(issue.Message, definition);
    /// </code>
    /// </remarks>
    public static class PawnDefinitionValidator
    {
        /// <summary>One problem found on a definition, phrased as a full sentence with its fix.</summary>
        public readonly struct Issue
        {
            /// <summary>What is wrong and, where possible, how to fix it.</summary>
            public readonly string Message;

            /// <summary>Creates an issue.</summary>
            /// <param name="message">The full-sentence description.</param>
            public Issue(string message)
            {
                Message = message;
            }
        }

        /// <summary>Validates <paramref name="definition"/> and returns everything wrong with it.</summary>
        /// <param name="definition">The asset to check. A <c>null</c> asset yields one issue rather than throwing.</param>
        /// <returns>The issues found, empty when the definition is sound.</returns>
        public static List<Issue> Validate(PawnDefinition definition)
        {
            var issues = new List<Issue>();
            if (definition == null)
            {
                issues.Add(new Issue("Definition is null."));
                return issues;
            }

            CheckForMissingManagedReferences(definition, issues);

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                issues.Add(new Issue("Id is empty. Save data resolves pawns by Id, so this pawn cannot be restored from a save."));
            }

            if (definition.MaxVital <= 0f)
            {
                issues.Add(new Issue("Max Vital must be greater than zero — a pawn with no vitals cannot spawn alive."));
            }

            if (definition.SpawnInvulnerability < 0f)
            {
                issues.Add(new Issue("Spawn Invulnerability is negative. Use zero to disable the grace period."));
            }

            CheckTags(definition, issues);
            CheckModifiers(definition, issues);

            return issues;
        }

        private static void CheckTags(PawnDefinition definition, List<Issue> issues)
        {
            IReadOnlyList<PawnTag> tags = definition.Tags;
            var seen = new HashSet<string>();

            for (int i = 0; i < tags.Count; i++)
            {
                PawnTag tag = tags[i];

                if (!tag.IsValid)
                {
                    issues.Add(new Issue($"Tag {i} is empty. Remove the entry or give it a dotted name such as 'Class.Rogue'."));
                    continue;
                }

                if (!seen.Add(tag.Value))
                {
                    issues.Add(new Issue($"Tag '{tag.Value}' is listed more than once. The duplicate has no effect."));
                }
            }
        }

        private static void CheckModifiers(PawnDefinition definition, List<Issue> issues)
        {
            IReadOnlyList<Vitals.IDamageModifier> modifiers = definition.DamageModifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == null)
                {
                    issues.Add(new Issue($"Damage Modifier {i} is empty. Pick a type from the dropdown or remove the entry."));
                }
            }
        }

        private static void CheckForMissingManagedReferences(PawnDefinition definition, List<Issue> issues)
        {
            var serializedObject = new SerializedObject(definition);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (property.propertyType != SerializedPropertyType.ManagedReference) continue;

                string fullTypename = property.managedReferenceFullTypename;
                bool looksAssigned = !string.IsNullOrEmpty(fullTypename) && fullTypename != "<null>";

                if (looksAssigned && property.managedReferenceValue == null)
                {
                    issues.Add(new Issue(
                        $"'{property.propertyPath}' references a missing or renamed type ('{fullTypename}'). " +
                        "Add a [MovedFrom] attribute to the renamed class or reassign this field."));
                }
            }
        }
    }
}
