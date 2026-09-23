using UnityEngine;

namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// Decides where a pawn appears. Implement it to express "furthest from any enemy", "the
    /// checkpoint the player last touched", "a random cell of the dungeon", or anything else.
    /// </summary>
    /// <remarks>
    /// Assign through <see cref="PawnSpawner.SpawnPointProvider"/>; the default is
    /// <see cref="TransformSpawnPointProvider"/> over the spawner's serialized list of points.
    /// <code>
    /// public sealed class SafestPointProvider : ISpawnPointProvider
    /// {
    ///     public bool TryGetSpawnPoint(Pawn prefab, out Vector3 position, out Quaternion rotation)
    ///     {
    ///         // pick the point with no hostile pawn within 20 units
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface ISpawnPointProvider
    {
        /// <summary>Picks a place for the next pawn to appear.</summary>
        /// <param name="prefab">The pawn about to be spawned, for providers that place different archetypes differently. May be <c>null</c>.</param>
        /// <param name="position">The chosen world-space position.</param>
        /// <param name="rotation">The chosen world-space rotation.</param>
        /// <returns>
        /// <c>false</c> when no point is available right now — every point occupied, every
        /// checkpoint unreached. The spawner then declines to spawn rather than stacking pawns.
        /// </returns>
        bool TryGetSpawnPoint(Pawn prefab, out Vector3 position, out Quaternion rotation);
    }
}
