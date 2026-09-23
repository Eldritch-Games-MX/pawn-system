using System.Collections.Generic;
using System.Linq;
using EldritchGames.PawnSystem.Identity;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.PawnSystem.Editor
{
    /// <summary>
    /// Project-wide sweep of every <see cref="PawnDefinition"/> asset, listing what
    /// <see cref="PawnDefinitionValidator"/> finds.
    /// </summary>
    /// <remarks>
    /// Open with <b>Eldritch Games &gt; Pawn System &gt; Validate Pawn Definitions</b>. Run it after
    /// renaming or moving any class used in a <c>[SerializeReference]</c> list — that is the failure
    /// that silently empties fields in existing assets.
    /// </remarks>
    public sealed class PawnDefinitionValidatorWindow : EditorWindow
    {
        private readonly List<(PawnDefinition definition, List<PawnDefinitionValidator.Issue> issues)> results =
            new List<(PawnDefinition, List<PawnDefinitionValidator.Issue>)>();

        private Vector2 scrollPosition;

        [MenuItem("Eldritch Games/Pawn System/Validate Pawn Definitions")]
        private static void Open()
        {
            var window = GetWindow<PawnDefinitionValidatorWindow>(true, "Pawn Definition Validator");
            window.RunValidation();
            window.Show();
        }

        private void RunValidation()
        {
            results.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:PawnDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<PawnDefinition>(path);
                if (definition == null) continue;

                results.Add((definition, PawnDefinitionValidator.Validate(definition)));
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Re-run Validation"))
            {
                RunValidation();
            }

            int totalIssues = results.Sum(r => r.issues.Count);
            EditorGUILayout.LabelField($"{results.Count} pawn definition(s) checked, {totalIssues} issue(s) found.");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach ((PawnDefinition definition, List<PawnDefinitionValidator.Issue> issues) in results)
            {
                if (issues.Count == 0) continue;

                EditorGUILayout.ObjectField(definition, typeof(PawnDefinition), false);
                foreach (PawnDefinitionValidator.Issue issue in issues)
                {
                    EditorGUILayout.HelpBox(issue.Message, MessageType.Warning);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
