using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.PawnSystem.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="PawnDefinition"/>: an icon header, grouped foldout sections,
    /// inline validation, and the icon as the asset's Project-window thumbnail.
    /// </summary>
    /// <remarks>
    /// The sections mirror how the runtime reads the asset — Identity, Vitals, Lifecycle — so what
    /// a designer sees matches what the pawn does. Validation is the same
    /// <see cref="PawnDefinitionValidator"/> the project-wide window runs, shown here per asset so
    /// problems surface while authoring rather than at play time.
    /// </remarks>
    [CustomEditor(typeof(PawnDefinition))]
    public sealed class PawnDefinitionEditor : UnityEditor.Editor
    {
        private const float HeaderIconSize = 48f;

        private SerializedProperty id;
        private SerializedProperty displayName;
        private SerializedProperty icon;
        private SerializedProperty team;
        private SerializedProperty tags;
        private SerializedProperty maxVital;
        private SerializedProperty damageModifiers;
        private SerializedProperty spawnInvulnerability;
        private SerializedProperty releaseOnDeath;
        private SerializedProperty deactivateOnDespawn;

        private bool identityExpanded = true;
        private bool vitalsExpanded = true;
        private bool lifecycleExpanded = true;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("id");
            displayName = serializedObject.FindProperty("displayName");
            icon = serializedObject.FindProperty("icon");
            team = serializedObject.FindProperty("team");
            tags = serializedObject.FindProperty("tags");
            maxVital = serializedObject.FindProperty("maxVital");
            damageModifiers = serializedObject.FindProperty("damageModifiers");
            spawnInvulnerability = serializedObject.FindProperty("spawnInvulnerability");
            releaseOnDeath = serializedObject.FindProperty("releaseOnDeath");
            deactivateOnDespawn = serializedObject.FindProperty("deactivateOnDespawn");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawIdentityHeader(target as PawnDefinition);

            DrawSection("Identity", ref identityExpanded, () =>
            {
                EditorGUILayout.PropertyField(id);
                EditorGUILayout.PropertyField(displayName);
                EditorGUILayout.PropertyField(icon);
                EditorGUILayout.PropertyField(team);
                EditorGUILayout.PropertyField(tags, true);

                if (team.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "No Team is assigned, so every relationship resolves to Neutral. Assign one if AI target selection or friendly fire rules depend on this pawn's side.",
                        MessageType.Info);
                }
            });

            DrawSection("Vitals", ref vitalsExpanded, () =>
            {
                EditorGUILayout.PropertyField(maxVital);
                EditorGUILayout.PropertyField(damageModifiers, true);

                EditorGUILayout.HelpBox(
                    "Modifiers run top to bottom, before any IDamageModifier components on the pawn. Order matters: a flat reduction before a percentage is not the same as after.",
                    MessageType.None);
            });

            DrawSection("Lifecycle", ref lifecycleExpanded, () =>
            {
                EditorGUILayout.PropertyField(spawnInvulnerability);
                EditorGUILayout.PropertyField(releaseOnDeath);
                EditorGUILayout.PropertyField(deactivateOnDespawn);

                if (!releaseOnDeath.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Release On Death is off, so a player or AI keeps driving this pawn after it dies. Commands are still dropped while the pawn is dead — turn this off only when something in the game acts through a corpse.",
                        MessageType.Info);
                }
            });

            serializedObject.ApplyModifiedProperties();

            DrawValidation(target as PawnDefinition);
        }

        /// <inheritdoc/>
        public override bool HasPreviewGUI() => GetIcon() != null;

        /// <inheritdoc/>
        public override void OnPreviewGUI(Rect region, GUIStyle background)
        {
            Sprite sprite = GetIcon();
            if (sprite == null || sprite.texture == null) return;

            GUI.DrawTexture(region, sprite.texture, ScaleMode.ScaleToFit);
        }

        /// <inheritdoc/>
        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            Sprite sprite = GetIcon();
            if (sprite == null || sprite.texture == null) return null;

            var preview = new Texture2D(width, height);
            EditorUtility.CopySerialized(sprite.texture, preview);
            return preview;
        }

        private void DrawIdentityHeader(PawnDefinition definition)
        {
            if (definition == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                Sprite sprite = GetIcon();
                if (sprite != null && sprite.texture != null)
                {
                    Rect iconRect = GUILayoutUtility.GetRect(HeaderIconSize, HeaderIconSize, GUILayout.Width(HeaderIconSize));
                    GUI.DrawTexture(iconRect, sprite.texture, ScaleMode.ScaleToFit);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    string title = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.name : definition.DisplayName;
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"Max vital {definition.MaxVital:0.##}  ·  {definition.Tags.Count} tag(s)  ·  {definition.DamageModifiers.Count} modifier(s)",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawValidation(PawnDefinition definition)
        {
            List<PawnDefinitionValidator.Issue> issues = PawnDefinitionValidator.Validate(definition);
            if (issues.Count == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            foreach (PawnDefinitionValidator.Issue issue in issues)
            {
                EditorGUILayout.HelpBox(issue.Message, MessageType.Warning);
            }
        }

        private static void DrawSection(string title, ref bool expanded, Action body)
        {
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            if (!expanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                body();
            }

            EditorGUILayout.Space();
        }

        private Sprite GetIcon() => icon != null ? icon.objectReferenceValue as Sprite : null;
    }
}
