using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// The set of pawns currently in play, with the queries every game ends up needing: who is on
    /// this team, who is tagged that, who is nearest to here.
    /// </summary>
    /// <remarks>
    /// A plain object, not a singleton — create one per world, per arena, or per level and hand it
    /// to whoever needs it. <see cref="PawnSpawner"/> owns one and registers everything it spawns;
    /// scene-placed pawns are registered by the game.
    /// <code>
    /// var registry = new PawnRegistry();
    /// registry.Register(scenePlacedPawn);
    ///
    /// var buffer = new List&lt;Pawn&gt;();
    /// registry.Query(buffer, team: monsters, state: PawnState.Alive);
    /// Pawn closest = registry.FindNearest(player.transform.position, p => p.IsHostileTo(player));
    /// </code>
    /// <para>
    /// A registered pawn removes itself when it despawns, so the registry never hands out corpses
    /// that have left the world. Pawns that are merely dead stay registered — looting and revival
    /// need to find them.
    /// </para>
    /// </remarks>
    public sealed class PawnRegistry
    {
        private readonly List<Pawn> pawns = new List<Pawn>();

        /// <summary>Every registered pawn, in registration order.</summary>
        public IReadOnlyList<Pawn> All => pawns;

        /// <summary>How many pawns are registered.</summary>
        public int Count => pawns.Count;

        /// <summary>Raised after a pawn is added.</summary>
        public event Action<Pawn> Registered;

        /// <summary>Raised after a pawn is removed, whether explicitly or by despawning.</summary>
        public event Action<Pawn> Unregistered;

        /// <summary>Adds <paramref name="pawn"/> and starts watching it for despawn.</summary>
        /// <param name="pawn">The pawn to track.</param>
        /// <returns><c>true</c> when the registry changed; <c>false</c> for <c>null</c> or a pawn already registered.</returns>
        public bool Register(Pawn pawn)
        {
            if (pawn == null || pawns.Contains(pawn)) return false;

            pawns.Add(pawn);
            pawn.Despawned += OnPawnDespawned;
            Registered?.Invoke(pawn);
            return true;
        }

        /// <summary>Removes <paramref name="pawn"/> and stops watching it.</summary>
        /// <param name="pawn">The pawn to stop tracking.</param>
        /// <returns><c>true</c> when the registry changed.</returns>
        public bool Unregister(Pawn pawn)
        {
            if (pawn == null || !pawns.Remove(pawn)) return false;

            pawn.Despawned -= OnPawnDespawned;
            Unregistered?.Invoke(pawn);
            return true;
        }

        /// <summary>Removes every pawn, raising <see cref="Unregistered"/> once per pawn.</summary>
        public void Clear()
        {
            for (int i = pawns.Count - 1; i >= 0; i--)
                Unregister(pawns[i]);
        }

        /// <summary>
        /// Fills <paramref name="results"/> with the registered pawns matching every filter given.
        /// </summary>
        /// <param name="results">Caller-owned buffer. Cleared before use, so reuse one list rather than allocating per query.</param>
        /// <param name="team">Only pawns currently on this team. <c>null</c> matches any team.</param>
        /// <param name="tag">Only pawns whose tags match this one hierarchically. Default matches any tags.</param>
        /// <param name="state">Only pawns in this lifecycle state. <c>null</c> matches any state.</param>
        /// <returns>How many pawns matched.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results"/> is <c>null</c>.</exception>
        public int Query(List<Pawn> results, TeamDefinition team = null, PawnTag tag = default, PawnState? state = null)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null) continue;
                if (team != null && pawn.Team != team) continue;
                if (tag.IsValid && !pawn.Tags.Has(tag)) continue;
                if (state.HasValue && pawn.State != state.Value) continue;

                results.Add(pawn);
            }

            return results.Count;
        }

        /// <summary>Returns the registered pawn closest to <paramref name="position"/>, or <c>null</c> when none matches.</summary>
        /// <param name="position">The world-space position to measure from.</param>
        /// <param name="filter">Optional extra test — hostility, line of sight, whatever the caller needs. <c>null</c> accepts every pawn.</param>
        /// <remarks>Compares squared distances and allocates nothing.</remarks>
        public Pawn FindNearest(Vector3 position, Func<Pawn, bool> filter = null)
        {
            Pawn nearest = null;
            float nearestSqr = float.PositiveInfinity;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null) continue;
                if (filter != null && !filter(pawn)) continue;

                float sqr = (pawn.transform.position - position).sqrMagnitude;
                if (sqr >= nearestSqr) continue;

                nearest = pawn;
                nearestSqr = sqr;
            }

            return nearest;
        }

        private void OnPawnDespawned(Pawn pawn) => Unregister(pawn);
    }
}
