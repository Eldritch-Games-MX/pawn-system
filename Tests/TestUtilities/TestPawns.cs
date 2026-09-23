using System.Reflection;
using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// Creates <see cref="Pawn"/> instances on throwaway GameObjects, with the private serialized
    /// fields a test needs to control already set.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Pawn pawn = TestPawns.Create(new PawnDefinitionBuilder().WithMaxVital(30f).Build());
    /// pawn.Spawn();
    /// // ...
    /// Object.DestroyImmediate(pawn.gameObject);
    /// </code>
    /// Automatic spawning is switched off, so an EditMode test controls the lifecycle explicitly
    /// and the same helper works in PlayMode where <c>Start</c> would otherwise run.
    /// The caller owns the GameObject and should destroy it in <c>TearDown</c>.
    /// </remarks>
    public static class TestPawns
    {
        /// <summary>Creates a pawn with a definition, ready to be spawned by the test.</summary>
        /// <param name="definition">The definition to give it, or <c>null</c> for a pawn with none.</param>
        /// <param name="name">Name for the GameObject, useful when a failure message names it.</param>
        /// <returns>The pawn component, on a new inactive-free GameObject at the origin.</returns>
        public static Pawn Create(PawnDefinition definition = null, string name = "Pawn")
        {
            var gameObject = new GameObject(name);
            Pawn pawn = gameObject.AddComponent<Pawn>();

            SetField(pawn, "definition", definition);
            SetField(pawn, "spawnOnStart", false);

            return pawn;
        }

        /// <summary>Creates a pawn and spawns it in place.</summary>
        /// <param name="definition">The definition to give it.</param>
        /// <param name="name">Name for the GameObject.</param>
        /// <returns>The spawned pawn.</returns>
        public static Pawn CreateSpawned(PawnDefinition definition = null, string name = "Pawn")
        {
            Pawn pawn = Create(definition ?? new PawnDefinitionBuilder().WithId(name).Build(), name);
            pawn.Spawn();
            return pawn;
        }

        private static void SetField(Pawn pawn, string fieldName, object value)
        {
            FieldInfo field = typeof(Pawn).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(pawn, value);
        }
    }
}
