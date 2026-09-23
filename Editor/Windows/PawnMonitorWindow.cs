using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Vitals;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.PawnSystem.Editor
{
    /// <summary>
    /// A live Play Mode dashboard listing every <see cref="Pawn"/> in the open scenes — state,
    /// team, vitals, possessor — with quick debug actions.
    /// </summary>
    /// <remarks>
    /// Open with <b>Eldritch Games &gt; Pawn System &gt; Pawn Monitor</b>. Where the
    /// <c>PawnDefinitionEditor</c> inspector helps author one asset, this window helps watch every
    /// pawn actually in play at once — useful the moment a scene has more than a couple, since
    /// stepping through them one Inspector selection at a time stops scaling quickly.
    /// </remarks>
    public sealed class PawnMonitorWindow : EditorWindow
    {
        private static readonly Color HealthyColor = new Color(0.3f, 0.75f, 0.35f);
        private static readonly Color HurtColor = new Color(0.85f, 0.65f, 0.15f);
        private static readonly Color CriticalColor = new Color(0.8f, 0.25f, 0.25f);

        private Vector2 scrollPosition;
        private string nameFilter = string.Empty;

        [MenuItem("Eldritch Games/Pawn System/Pawn Monitor")]
        private static void Open()
        {
            var window = GetWindow<PawnMonitorWindow>("Pawn Monitor");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live pawns.", MessageType.Info);
                return;
            }

            Pawn[] pawns = Object.FindObjectsByType<Pawn>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"{pawns.Length} pawn(s)", EditorStyles.toolbarButton, GUILayout.Width(90));
                nameFilter = EditorGUILayout.TextField(nameFilter, EditorStyles.toolbarSearchField);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < pawns.Length; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null) continue;
                if (!string.IsNullOrEmpty(nameFilter) && pawn.name.IndexOf(nameFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                DrawPawnRow(pawn);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPawnRow(Pawn pawn)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(pawn.name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true)))
                    {
                        EditorGUIUtility.PingObject(pawn.gameObject);
                        Selection.activeGameObject = pawn.gameObject;
                    }

                    GUILayout.Label(pawn.State.ToString(), GUILayout.Width(90));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    TeamDefinition team = pawn.Team;
                    GUILayout.Label(team != null ? team.DisplayName : "(no team)", GUILayout.Width(120));
                    GUILayout.Label(DescribePossessor(pawn), GUILayout.Width(160));
                }

                DrawVitalsBar(pawn);
                DrawQuickActions(pawn);
            }
        }

        private static string DescribePossessor(Pawn pawn) =>
            pawn.IsPossessed ? pawn.CurrentPossessor.GetType().Name : "(unpossessed)";

        private static void DrawVitalsBar(Pawn pawn)
        {
            if (pawn.State == PawnState.Unspawned || pawn.State == PawnState.Despawned)
            {
                EditorGUILayout.LabelField("Vitals: —");
                return;
            }

            float current = pawn.Vitals.Current;
            float max = pawn.Vitals.Max;
            float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            Rect rect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, fraction, $"{current:0.#} / {max:0.#}");

            Color previous = GUI.color;
            GUI.color = fraction > 0.5f ? HealthyColor : fraction > 0.2f ? HurtColor : CriticalColor;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * fraction, rect.height), GUI.color * new Color(1f, 1f, 1f, 0.25f));
            GUI.color = previous;
        }

        private static void DrawQuickActions(Pawn pawn)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!pawn.IsAlive))
                {
                    if (GUILayout.Button("Kill")) pawn.Kill();
                }

                using (new EditorGUI.DisabledScope(pawn.State != PawnState.Dead && pawn.State != PawnState.Incapacitated))
                {
                    if (GUILayout.Button("Revive")) pawn.TryRevive(ReviveInfo.Full);
                }

                using (new EditorGUI.DisabledScope(pawn.State == PawnState.Despawned))
                {
                    if (GUILayout.Button("Despawn")) pawn.Despawn();
                }
            }
        }
    }
}
