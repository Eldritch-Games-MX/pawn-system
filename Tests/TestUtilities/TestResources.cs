using System.Reflection;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>Creates <see cref="ResourceDefinition"/> assets in memory.</summary>
    /// <remarks>
    /// <code>
    /// ResourceDefinition mana = TestResources.Create("mana", defaultMax: 50f);
    /// </code>
    /// </remarks>
    public static class TestResources
    {
        /// <summary>Creates a resource definition.</summary>
        /// <param name="id">The save-data identifier, also used as the display name.</param>
        /// <param name="defaultMax">The maximum, and starting, value.</param>
        /// <returns>The in-memory resource asset.</returns>
        public static ResourceDefinition Create(string id, float defaultMax = 100f)
        {
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            SetField(resource, "id", id);
            SetField(resource, "displayName", id);
            SetField(resource, "defaultMax", defaultMax);
            return resource;
        }

        private static void SetField(ResourceDefinition resource, string fieldName, object value)
        {
            FieldInfo field = typeof(ResourceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(resource, value);
        }
    }
}
