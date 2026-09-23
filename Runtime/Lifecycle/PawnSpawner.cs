using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// Brings pawns into the world and brings them back after they die: instantiate or rent from a
    /// pool, place through a spawn-point provider, register, and respawn on the schedule a
    /// respawn policy sets.
    /// </summary>
    /// <remarks>
    /// Everything it does is delegated to a seam, so the same component serves an arena shooter, a
    /// wave defence and a turn-based skirmish:
    /// <see cref="SpawnPointProvider"/> decides <em>where</em>, <see cref="RespawnPolicy"/> decides
    /// <em>whether and when</em>, <see cref="Pool"/> decides <em>from where</em>.
    /// <code>
    /// spawner.Pool = new SimplePawnPool(poolRoot);
    /// spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 3f, maxRespawns: 5);
    ///
    /// Pawn enemy = spawner.Spawn();
    /// enemy.TryPossess(new WanderingBrain());
    ///
    /// void Update() => spawner.Tick(Time.deltaTime);   // drives pending respawns
    /// </code>
    /// <para>
    /// Like every other type in this package the spawner has no <c>Update</c> of its own — call
    /// <see cref="Tick"/> from the game's clock, once per frame or once per round.
    /// </para>
    /// </remarks>
    public sealed class PawnSpawner : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("The pawn prefab to spawn. Can also be set per call at runtime.")]
        [SerializeField] private Pawn prefab;

        [Tooltip("Spawn points, used round-robin by the default provider. Leave empty to spawn at this GameObject.")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Header("Configuration")]
        [Tooltip("How many pawns to spawn on Start. Zero leaves spawning entirely to code.")]
        [SerializeField] private int initialSpawnCount;

        [Tooltip("Schedule a respawn when a pawn this spawner owns dies, subject to the respawn policy.")]
        [SerializeField] private bool respawnOnDeath = true;

        [Tooltip("Delay used by the default respawn policy, in the same units passed to Tick.")]
        [SerializeField] private float respawnDelay = 5f;

        private readonly PawnRegistry registry = new PawnRegistry();
        private readonly List<PendingRespawn> pending = new List<PendingRespawn>();

        private ISpawnPointProvider spawnPointProvider;
        private IRespawnPolicy respawnPolicy;

        /// <summary>The pawn prefab spawned by default. Assignable at runtime to change what comes next.</summary>
        public Pawn Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        /// <summary>Every pawn this spawner currently has in play. Pawns remove themselves when they despawn.</summary>
        public PawnRegistry Registry => registry;

        /// <summary>
        /// Decides where each pawn appears. Defaults to a <see cref="TransformSpawnPointProvider"/>
        /// over the serialized spawn points.
        /// </summary>
        /// <exception cref="ArgumentNullException">A <c>null</c> provider was assigned.</exception>
        public ISpawnPointProvider SpawnPointProvider
        {
            get => spawnPointProvider ??= new TransformSpawnPointProvider(spawnPoints);
            set => spawnPointProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Decides whether and when a dead pawn comes back. Defaults to a
        /// <see cref="DelayedRespawnPolicy"/> using the serialized delay.
        /// </summary>
        /// <exception cref="ArgumentNullException">A <c>null</c> policy was assigned.</exception>
        public IRespawnPolicy RespawnPolicy
        {
            get => respawnPolicy ??= new DelayedRespawnPolicy(respawnDelay);
            set => respawnPolicy = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Where instances come from. <c>null</c> — the default — instantiates on spawn and destroys
        /// on despawn.
        /// </summary>
        public IPawnPool Pool { get; set; }

        /// <summary>How many dead pawns are waiting to come back.</summary>
        public int PendingRespawnCount => pending.Count;

        /// <summary>Raised after this spawner puts a pawn into play.</summary>
        public event Action<Pawn> PawnSpawned;

        /// <summary>Raised after this spawner takes a pawn out of play.</summary>
        public event Action<Pawn> PawnDespawned;

        /// <summary>Spawns <see cref="Prefab"/> at the next point the provider offers.</summary>
        /// <returns>The pawn, or <c>null</c> when there is no prefab or the provider had no point available.</returns>
        public Pawn Spawn()
        {
            if (prefab == null) return null;
            if (!SpawnPointProvider.TryGetSpawnPoint(prefab, out Vector3 position, out Quaternion rotation))
                return null;

            return Spawn(position, rotation);
        }

        /// <summary>Spawns <see cref="Prefab"/> at an exact place, bypassing the spawn-point provider.</summary>
        /// <param name="position">Where to place the pawn.</param>
        /// <param name="rotation">Which way to face it.</param>
        /// <returns>The pawn, or <c>null</c> when there is no prefab or the pool declined to provide an instance.</returns>
        /// <remarks>
        /// The instance is registered, watched for death, and raised through <see cref="PawnSpawned"/>
        /// only after <see cref="Pawn.Spawn(Vector3, Quaternion)"/> has succeeded.
        /// </remarks>
        public Pawn Spawn(Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            Pawn pawn = Pool != null ? Pool.Rent(prefab) : Instantiate(prefab);
            if (pawn == null) return null;

            if (pawn.Spawn(position, rotation) != SpawnResult.Success)
            {
                // A pooled instance can come back dead or already in play; reset it and retry once.
                pawn.Despawn();
                if (pawn.Spawn(position, rotation) != SpawnResult.Success) return null;
            }

            pawn.Died += OnPawnDied;
            registry.Register(pawn);
            PawnSpawned?.Invoke(pawn);
            return pawn;
        }

        /// <summary>
        /// Takes <paramref name="pawn"/> out of play and either returns it to the pool or destroys it.
        /// </summary>
        /// <param name="pawn">The pawn to remove. <c>null</c> and pawns this spawner does not own are ignored.</param>
        public void Despawn(Pawn pawn)
        {
            if (pawn == null) return;

            pawn.Died -= OnPawnDied;
            CancelRespawn(pawn);
            pawn.Despawn();
            registry.Unregister(pawn);
            PawnDespawned?.Invoke(pawn);

            if (Pool != null) Pool.Return(pawn);
            else DestroyPawn(pawn);
        }

        private static void DestroyPawn(Pawn pawn)
        {
            // Destroy is a play-mode-only call; edit-mode tooling and EditMode tests need the immediate form.
            if (Application.isPlaying) Destroy(pawn.gameObject);
            else DestroyImmediate(pawn.gameObject);
        }

        /// <summary>Takes every pawn this spawner owns out of play and clears pending respawns.</summary>
        public void DespawnAll()
        {
            pending.Clear();

            IReadOnlyList<Pawn> live = registry.All;
            for (int i = live.Count - 1; i >= 0; i--)
                Despawn(live[i]);
        }

        /// <summary>Queues <paramref name="pawn"/> to come back after <paramref name="delay"/>.</summary>
        /// <param name="pawn">The dead pawn to bring back.</param>
        /// <param name="delay">How long to wait, in the units passed to <see cref="Tick"/>. Zero respawns on the next tick.</param>
        /// <remarks>
        /// Respawning despawns the pawn and spawns it again at a fresh point, so it returns with full
        /// vitals, its definition's tags and team, and the spawn grace period — the same state a
        /// newly spawned pawn has. A pawn already queued is not queued twice.
        /// </remarks>
        public void ScheduleRespawn(Pawn pawn, float delay)
        {
            if (pawn == null) return;

            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Pawn == pawn)
                    return;

            pending.Add(new PendingRespawn(pawn, delay < 0f ? 0f : delay));
        }

        /// <summary>Removes <paramref name="pawn"/> from the respawn queue, if it is in it.</summary>
        /// <param name="pawn">The pawn to stop waiting for. No-ops when it is not queued.</param>
        public void CancelRespawn(Pawn pawn)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].Pawn == pawn)
                    pending.RemoveAt(i);
        }

        /// <summary>
        /// Advances pending respawns by <paramref name="amount"/> and brings back everything whose
        /// wait has elapsed.
        /// </summary>
        /// <param name="amount">Elapsed time — seconds for a real-time game, rounds for a turn-based one.</param>
        public void Tick(float amount)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                PendingRespawn entry = pending[i];
                float remaining = entry.Remaining - amount;

                if (remaining > 0f)
                {
                    pending[i] = new PendingRespawn(entry.Pawn, remaining);
                    continue;
                }

                pending.RemoveAt(i);
                Respawn(entry.Pawn);
            }
        }

        private void Start()
        {
            for (int i = 0; i < initialSpawnCount; i++) Spawn();
        }

        private void Respawn(Pawn pawn)
        {
            if (pawn == null) return;

            if (!SpawnPointProvider.TryGetSpawnPoint(prefab, out Vector3 position, out Quaternion rotation))
            {
                position = pawn.transform.position;
                rotation = pawn.transform.rotation;
            }

            pawn.Despawn();
            registry.Unregister(pawn);

            if (pawn.Spawn(position, rotation) != SpawnResult.Success) return;

            registry.Register(pawn);
            PawnSpawned?.Invoke(pawn);
        }

        private void OnPawnDied(Pawn pawn, DeathInfo info)
        {
            if (!respawnOnDeath) return;
            if (!RespawnPolicy.ShouldRespawn(pawn)) return;

            ScheduleRespawn(pawn, RespawnPolicy.GetDelay(pawn));
        }

        /// <summary>One pawn waiting to come back, and how long it still has to wait.</summary>
        private readonly struct PendingRespawn
        {
            /// <summary>Creates a queued respawn.</summary>
            /// <param name="pawn">The pawn waiting to come back.</param>
            /// <param name="remaining">How much of the wait is left.</param>
            public PendingRespawn(Pawn pawn, float remaining)
            {
                Pawn = pawn;
                Remaining = remaining;
            }

            /// <summary>The pawn waiting to come back.</summary>
            public Pawn Pawn { get; }

            /// <summary>How much of the wait is left, in the units passed to Tick.</summary>
            public float Remaining { get; }
        }
    }
}
