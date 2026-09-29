using System.Reflection;
using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// Creates <see cref="Actor"/> instances on throwaway GameObjects, with the private serialized
    /// fields a test needs to control already set. See <see cref="TestPawns"/> for the possessable
    /// counterpart.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Actor actor = TestActors.Create(new ActorDefinitionBuilder().WithMaxVital(30f).Build());
    /// actor.Spawn();
    /// // ...
    /// Object.DestroyImmediate(actor.gameObject);
    /// </code>
    /// Automatic spawning is switched off, so an EditMode test controls the lifecycle explicitly.
    /// The caller owns the GameObject and should destroy it in <c>TearDown</c>.
    /// </remarks>
    public static class TestActors
    {
        /// <summary>Creates an actor with a definition, ready to be spawned by the test.</summary>
        /// <param name="definition">The definition to give it, or <c>null</c> for an actor with none.</param>
        /// <param name="name">Name for the GameObject, useful when a failure message names it.</param>
        /// <returns>The actor component, on a new GameObject at the origin.</returns>
        public static Actor Create(ActorDefinition definition = null, string name = "Actor")
        {
            var gameObject = new GameObject(name);
            Actor actor = gameObject.AddComponent<Actor>();

            SetField(actor, "definition", definition);
            SetField(actor, "spawnOnStart", false);

            return actor;
        }

        /// <summary>Creates an actor and spawns it in place.</summary>
        /// <param name="definition">The definition to give it.</param>
        /// <param name="name">Name for the GameObject.</param>
        /// <returns>The spawned actor.</returns>
        public static Actor CreateSpawned(ActorDefinition definition = null, string name = "Actor")
        {
            Actor actor = Create(definition ?? new ActorDefinitionBuilder().WithId(name).Build(), name);
            actor.Spawn();
            return actor;
        }

        private static void SetField(Actor actor, string fieldName, object value)
        {
            FieldInfo field = typeof(Actor).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(actor, value);
        }
    }
}
