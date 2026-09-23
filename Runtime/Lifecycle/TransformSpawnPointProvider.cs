using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// The built-in <see cref="ISpawnPointProvider"/>: cycles round-robin through a list of
    /// Transforms, so successive spawns spread across the points instead of piling onto one.
    /// </summary>
    /// <remarks>
    /// Round-robin rather than random because it is deterministic — the same sequence of spawns
    /// lands in the same places in a test as in a build. Games wanting randomness, occupancy checks
    /// or distance rules implement their own provider.
    /// <code>
    /// spawner.SpawnPointProvider = new TransformSpawnPointProvider(arenaCorners);
    /// </code>
    /// Null entries are skipped. An empty or all-null list makes
    /// <see cref="TryGetSpawnPoint"/> return <c>false</c>, which stops the spawner rather than
    /// dropping pawns at the origin.
    /// </remarks>
    public sealed class TransformSpawnPointProvider : ISpawnPointProvider
    {
        private readonly IReadOnlyList<Transform> points;
        private int next;

        /// <summary>Creates a provider over <paramref name="points"/>.</summary>
        /// <param name="points">The spawn points, used in order. May be <c>null</c> or empty.</param>
        public TransformSpawnPointProvider(IReadOnlyList<Transform> points)
        {
            this.points = points;
        }

        /// <inheritdoc/>
        public bool TryGetSpawnPoint(Pawn prefab, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (points == null || points.Count == 0) return false;

            for (int attempt = 0; attempt < points.Count; attempt++)
            {
                Transform point = points[next % points.Count];
                next = (next + 1) % points.Count;

                if (point == null) continue;

                position = point.position;
                rotation = point.rotation;
                return true;
            }

            return false;
        }
    }
}
