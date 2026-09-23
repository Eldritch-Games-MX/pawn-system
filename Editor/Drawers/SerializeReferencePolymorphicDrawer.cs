using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.PawnSystem.Editor
{
    /// <summary>
    /// Draws a <c>[SerializeReference]</c> field marked with
    /// <see cref="SerializeReferenceDropdownAttribute"/> as a type picker listing every
    /// implementation of the field's interface.
    /// </summary>
    /// <remarks>
    /// This is what makes <see cref="Identity.PawnDefinition.DamageModifiers"/> authorable: a
    /// downstream project drops a <c>[Serializable]</c> class implementing
    /// <see cref="Vitals.IDamageModifier"/> into its own assembly and it appears in the dropdown
    /// automatically, annotated with its <see cref="PawnDocAttribute"/> summary.
    /// <para>
    /// Candidate types come from <see cref="TypeCache"/>: non-abstract, non-generic, with a public
    /// parameterless constructor, ordered by name.
    /// </para>
    /// </remarks>
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public sealed class SerializeReferencePolymorphicDrawer : PropertyDrawer
    {
        private const float DropdownHeight = 20f;
        private const float SummaryHeight = 16f;
        private const float Spacing = 2f;

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var dropdownRect = new Rect(position.x, position.y, position.width, DropdownHeight);
            string currentTypeName = GetShortTypeName(property.managedReferenceFullTypename);
            var buttonLabel = new GUIContent(string.IsNullOrEmpty(currentTypeName) ? "(None)" : currentTypeName);

            if (EditorGUI.DropdownButton(dropdownRect, buttonLabel, FocusType.Keyboard))
            {
                ShowTypeMenu(property);
            }

            float y = position.y + DropdownHeight + Spacing;

            string summary = GetSummary(property.managedReferenceValue);
            if (!string.IsNullOrEmpty(summary))
            {
                var summaryRect = new Rect(position.x, y, position.width, SummaryHeight);
                EditorGUI.LabelField(summaryRect, summary, GetSummaryStyle());
                y += SummaryHeight + Spacing;
            }

            if (property.managedReferenceValue != null)
            {
                var fieldRect = new Rect(position.x, y, position.width, position.y + position.height - y);
                EditorGUI.PropertyField(fieldRect, property, label, true);
            }
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float height = DropdownHeight;

            if (!string.IsNullOrEmpty(GetSummary(property.managedReferenceValue)))
            {
                height += Spacing + SummaryHeight;
            }

            if (property.managedReferenceValue != null)
            {
                height += Spacing + EditorGUI.GetPropertyHeight(property, label, true);
            }

            return height;
        }

        private void ShowTypeMenu(SerializedProperty property)
        {
            Type elementType = ResolveElementType();
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("(None)"), property.managedReferenceValue == null, () => AssignType(property, null));

            foreach (Type candidateType in GetCandidateTypes(elementType))
            {
                Type type = candidateType;
                bool isCurrent = property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;
                menu.AddItem(new GUIContent(BuildMenuLabel(type)), isCurrent, () => AssignType(property, type));
            }

            menu.ShowAsContext();
        }

        private static void AssignType(SerializedProperty property, Type type)
        {
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.serializedObject.ApplyModifiedProperties();
        }

        private Type ResolveElementType()
        {
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(fieldType))
            {
                return fieldType.GetGenericArguments()[0];
            }
            return fieldType;
        }

        private static IEnumerable<Type> GetCandidateTypes(Type elementType)
        {
            if (elementType == null) return Enumerable.Empty<Type>();

            return TypeCache.GetTypesDerivedFrom(elementType)
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name);
        }

        private static string BuildMenuLabel(Type type)
        {
            string summary = type.GetCustomAttribute<PawnDocAttribute>()?.Summary;
            return string.IsNullOrEmpty(summary) ? type.Name : $"{type.Name}  —  {summary}";
        }

        private static GUIStyle GetSummaryStyle()
        {
            return new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontStyle = FontStyle.Italic };
        }

        private static string GetSummary(object managedReferenceValue)
        {
            return managedReferenceValue?.GetType().GetCustomAttribute<PawnDocAttribute>()?.Summary;
        }

        private static string GetShortTypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename)) return null;
            int spaceIndex = managedReferenceFullTypename.IndexOf(' ');
            string typeName = spaceIndex >= 0 ? managedReferenceFullTypename.Substring(spaceIndex + 1) : managedReferenceFullTypename;
            int lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
        }
    }
}
