using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Movement;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// A character in the world — anything a player or an AI can drive. Owns an identity, a
    /// lifecycle (spawn, death, revival, despawn), a possessor, vitals, and whatever capabilities
    /// the game has composed onto its GameObject.
    /// </summary>
    /// <remarks>
    /// One component per character, and the only MonoBehaviour this package requires. Everything
    /// genre-specific is a capability the game adds as a separate component and reaches through
    /// <see cref="TryGet{T}"/> — the pawn itself knows nothing about weapons, dialogue, stealth or
    /// turn order.
    /// <code>
    /// var pawn = Instantiate(knightPrefab).GetComponent&lt;Pawn&gt;();
    /// pawn.Spawn(spawnPoint.position, spawnPoint.rotation);
    /// pawn.TryPossess(playerPossessor);     // or an AI brain — the pawn cannot tell
    ///
    /// pawn.ApplyDamage(new DamageInfo(25f, fireType, instigator: dragon));
    /// pawn.TryRevive(ReviveInfo.Full);
    /// </code>
    /// <para>
    /// Time is pushed in, never pulled: the pawn has no <c>Update</c>. Call <see cref="Tick"/>
    /// once per frame for a real-time game, or once per round for a turn-based one, and the same
    /// pawn works in both.
    /// </para>
    /// <para>
    /// Every state change is transactional in the same sense as the Eldritch Ability System: all
    /// validation happens before any mutation, so a call that returns a failure code has changed
    /// nothing and raised no events.
    /// </para>
    /// </remarks>
    public sealed class Pawn : MonoBehaviour, IDamageable
    {
        [Header("Bindings")]
        [Tooltip("The authored recipe for this pawn: identity, team, vitals and death behaviour.")]
        [SerializeField] private PawnDefinition definition;

        [Tooltip("Eyes or aim origin, used by cameras, AI perception and targeting. Defaults to this Transform.")]
        [SerializeField] private Transform viewPoint;

        [Header("Configuration")]
        [Tooltip("Spawn in place on Start when nothing has spawned this pawn yet. Turn off for pawns a spawner or pool owns.")]
        [SerializeField] private bool spawnOnStart = true;

        private readonly PawnTagContainer tags = new PawnTagContainer();
        private readonly List<IDamageModifier> componentModifiers = new List<IDamageModifier>();

        private readonly PawnResourcePool resources = new PawnResourcePool();

        private IVitalSource vitals;
        private IPawnMotor motor;
        private ITeamResolver teamResolver = new DefaultTeamResolver();
        private IPawnAuthority authority;
        private float invulnerabilityRemaining;
        private bool invulnerableIndefinitely;
        private bool applyingDamage;
        private float incapacitationRemaining;
        private DeathInfo incapacitationCause;

        /// <summary>The authored recipe this pawn was built from, or <c>null</c> when none is assigned.</summary>
        public PawnDefinition Definition => definition;

        /// <summary>Where this pawn is in its lifecycle. Changed only through the methods on this class.</summary>
        public PawnState State { get; private set; } = PawnState.Unspawned;

        /// <summary><c>true</c> when the pawn is in play and not dead — the only state in which it can act or be hurt.</summary>
        public bool IsAlive => State == PawnState.Alive;

        /// <summary><c>true</c> when the pawn is downed but not dead. See <see cref="PawnState.Incapacitated"/>.</summary>
        public bool IsIncapacitated => State == PawnState.Incapacitated;

        /// <summary>The pawn's runtime tags, seeded from <see cref="PawnDefinition.Tags"/> when it spawns.</summary>
        public PawnTagContainer Tags => tags;

        /// <summary>
        /// The side this pawn is currently on. Set from <see cref="PawnDefinition.Team"/> on spawn
        /// and freely reassignable — mind control and defections are a plain assignment.
        /// </summary>
        public TeamDefinition Team { get; set; }

        /// <summary>
        /// Eyes or aim origin: where the pawn looks from, what a camera follows, what AI perception
        /// traces from. Falls back to this Transform when no view point is assigned.
        /// </summary>
        public Transform ViewPoint => viewPoint != null ? viewPoint : transform;

        /// <summary>
        /// Decides how this pawn regards others. Defaults to <see cref="DefaultTeamResolver"/>;
        /// assign your own to express reputation, truces or free-for-all rules.
        /// </summary>
        /// <exception cref="ArgumentNullException">A <c>null</c> resolver was assigned.</exception>
        public ITeamResolver TeamResolver
        {
            get => teamResolver;
            set => teamResolver = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The pool this pawn dies when it runs out of. Built from
        /// <see cref="PawnDefinition.MaxVital"/> on first use unless <see cref="SetVitalSource"/>
        /// replaced it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No vital source was assigned and there is no <see cref="PawnDefinition"/> to build one from.
        /// </exception>
        public IVitalSource Vitals => vitals ?? BuildVitals();

        /// <summary>
        /// How this pawn moves, or <c>null</c> when it has no motor. Resolved from this GameObject
        /// or its children the first time it is asked for.
        /// </summary>
        /// <remarks>Movement is entirely optional — a turn-based portrait or a turret is a pawn with no motor.</remarks>
        public IPawnMotor Motor => motor ??= GetComponentInChildren<IPawnMotor>(true);

        /// <summary>
        /// Secondary resource pools beyond <see cref="Vitals"/> — mana, stamina, ammunition,
        /// whatever a game wants that is not the number the pawn dies when it runs out of. Empty
        /// until something registers a resource, either from <see cref="PawnDefinition.InitialResources"/>
        /// on spawn or by calling <see cref="PawnResourcePool.Register(ResourceDefinition)"/> directly.
        /// </summary>
        public PawnResourcePool Resources => resources;

        /// <summary>Whoever is currently driving this pawn, or <c>null</c> when nothing is.</summary>
        public IPawnPossessor CurrentPossessor { get; private set; }

        /// <summary><c>true</c> when something is currently driving this pawn.</summary>
        public bool IsPossessed => CurrentPossessor != null;

        /// <summary>
        /// Marks who is authoritative over this pawn in a networked game. <c>null</c> — the
        /// default — means no networking layer is involved.
        /// </summary>
        /// <remarks>
        /// A data-only marker; nothing in this package reads it before acting. See
        /// <see cref="IPawnAuthority"/> for how a networked adapter is expected to use it.
        /// </remarks>
        public IPawnAuthority Authority
        {
            get => authority;
            set => authority = value;
        }

        /// <summary>Whether the current caller is authoritative over this pawn. <c>true</c> whenever <see cref="Authority"/> is unset.</summary>
        public bool HasAuthority => authority == null || authority.HasAuthority;

        /// <summary>
        /// <c>true</c> while damage is being ignored — during a spawn or revival grace period, or
        /// because <see cref="SetInvulnerable(bool)"/> was set.
        /// </summary>
        /// <remarks>
        /// Invulnerability never protects against <see cref="Kill"/>, nor against damage whose
        /// <see cref="DamageTypeDefinition.IgnoresInvulnerability"/> is set.
        /// </remarks>
        public bool IsInvulnerable => invulnerableIndefinitely || invulnerabilityRemaining > 0f;

        /// <summary>Seconds of grace remaining, or zero when the pawn is not on a timed invulnerability.</summary>
        public float InvulnerabilityRemaining => invulnerabilityRemaining;

        /// <summary>
        /// Seconds before a downed pawn bleeds out, or zero when the pawn is not incapacitated or
        /// has no bleed-out timer. Drive a bleed-out UI bar from this.
        /// </summary>
        public float IncapacitationRemaining => incapacitationRemaining;

        /// <summary>Raised after the pawn enters play and is <see cref="PawnState.Alive"/>.</summary>
        public event Action<Pawn> Spawned;

        /// <summary>Raised after the pawn leaves play and is <see cref="PawnState.Despawned"/>.</summary>
        public event Action<Pawn> Despawned;

        /// <summary>Raised after a dead pawn is brought back and is <see cref="PawnState.Alive"/> again.</summary>
        public event Action<Pawn> Revived;

        /// <summary>
        /// Raised for every hit that reaches the pawn, carrying the damage <em>after</em> the
        /// modifier pipeline — including fully absorbed hits, which arrive with an amount of zero.
        /// </summary>
        public event Action<Pawn, DamageInfo> DamageTaken;

        /// <summary>
        /// Raised after the pawn dies, with the record of what killed it. Fires exactly once per
        /// death; a revived pawn that dies again fires it again.
        /// </summary>
        public event Action<Pawn, DeathInfo> Died;

        /// <summary>
        /// Raised when a fatal blow downs an incapacitation-capable pawn instead of killing it —
        /// see <see cref="PawnState.Incapacitated"/>. The pawn is still addressable;
        /// <see cref="TryRevive"/> also recovers an incapacitated pawn back to
        /// <see cref="PawnState.Alive"/>.
        /// </summary>
        public event Action<Pawn, DeathInfo> Incapacitated;

        /// <summary>Raised after a possessor takes this pawn.</summary>
        public event Action<Pawn, IPawnPossessor> Possessed;

        /// <summary>Raised after a possessor gives up this pawn, with the possessor that left.</summary>
        public event Action<Pawn, IPawnPossessor> Released;

        /// <summary>Spawns the pawn where it already stands.</summary>
        /// <returns>The outcome; see <see cref="SpawnResult"/>.</returns>
        public SpawnResult Spawn() => Spawn(transform.position, transform.rotation);

        /// <summary>
        /// Brings the pawn into play at <paramref name="position"/>: reactivates the GameObject,
        /// resets team and tags from the definition, fills vitals, registers or refills
        /// <see cref="PawnDefinition.InitialResources"/>, grants the spawn grace period, and
        /// raises <see cref="Spawned"/>.
        /// </summary>
        /// <param name="position">Where to place the pawn.</param>
        /// <param name="rotation">Which way to face it.</param>
        /// <returns>
        /// <see cref="SpawnResult.Success"/>, <see cref="SpawnResult.AlreadySpawned"/> when the pawn
        /// is already in play, or <see cref="SpawnResult.MissingDefinition"/> when it has neither a
        /// definition nor an injected vital source. Both failures change nothing.
        /// </returns>
        /// <remarks>
        /// Placement goes through <see cref="IPawnMotor.Teleport"/> when the pawn has a motor, so a
        /// physics controller is moved correctly rather than having its transform overwritten.
        /// </remarks>
        public SpawnResult Spawn(Vector3 position, Quaternion rotation)
        {
            if (State == PawnState.Alive || State == PawnState.Dead || State == PawnState.Incapacitated) return SpawnResult.AlreadySpawned;
            if (definition == null && vitals == null) return SpawnResult.MissingDefinition;

            gameObject.SetActive(true);

            if (Motor != null) Motor.Teleport(position, rotation);
            else transform.SetPositionAndRotation(position, rotation);

            tags.Clear();
            if (definition != null)
            {
                Team = definition.Team;
                IReadOnlyList<PawnTag> authored = definition.Tags;
                for (int i = 0; i < authored.Count; i++) tags.Add(authored[i]);

                IReadOnlyList<ResourceDefinition> initialResources = definition.InitialResources;
                for (int i = 0; i < initialResources.Count; i++)
                {
                    ResourceDefinition resourceDefinition = initialResources[i];
                    if (resourceDefinition != null) resources.RegisterOrRefill(resourceDefinition);
                }
            }

            Vitals.Fill(1f);
            RefreshComponentModifiers();

            invulnerableIndefinitely = false;
            invulnerabilityRemaining = definition != null ? Mathf.Max(0f, definition.SpawnInvulnerability) : 0f;
            incapacitationRemaining = 0f;

            State = PawnState.Alive;
            Spawned?.Invoke(this);
            return SpawnResult.Success;
        }

        /// <summary>
        /// Takes the pawn out of play: releases its possessor, deactivates the GameObject when the
        /// definition says to, and raises <see cref="Despawned"/>.
        /// </summary>
        /// <remarks>
        /// Despawning never destroys anything — destruction or pooling is the caller's decision, so
        /// a pooled pawn can be spawned again later. No-ops when the pawn is already despawned.
        /// </remarks>
        public void Despawn()
        {
            if (State == PawnState.Despawned) return;

            ReleaseInternal();
            State = PawnState.Despawned;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = 0f;
            incapacitationRemaining = 0f;

            if (definition == null || definition.DeactivateOnDespawn) gameObject.SetActive(false);

            Despawned?.Invoke(this);
        }

        /// <summary>
        /// Hands the pawn to <paramref name="possessor"/>, failing if anything already holds it.
        /// </summary>
        /// <param name="possessor">The player controller, AI brain or other driver taking the pawn.</param>
        /// <returns>The outcome; see <see cref="PossessionResult"/>. Every failure changes nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="possessor"/> is <c>null</c>. Use <see cref="Release"/> to clear a possessor.</exception>
        /// <remarks>
        /// On success <see cref="CurrentPossessor"/> is set first, then
        /// <see cref="IPawnPossessor.OnPossessed"/> runs, then <see cref="Possessed"/> is raised —
        /// so a possessor can already use the pawn from inside its own callback. If that callback
        /// throws, the possession is rolled back and the exception propagates.
        /// </remarks>
        public PossessionResult TryPossess(IPawnPossessor possessor)
        {
            if (possessor == null) throw new ArgumentNullException(nameof(possessor));

            if (ReferenceEquals(CurrentPossessor, possessor)) return PossessionResult.AlreadyOwner;
            if (State == PawnState.Unspawned || State == PawnState.Despawned) return PossessionResult.NotSpawned;
            if (State == PawnState.Dead) return PossessionResult.PawnDead;
            if (State == PawnState.Incapacitated) return PossessionResult.PawnIncapacitated;
            if (CurrentPossessor != null) return PossessionResult.AlreadyPossessed;

            CurrentPossessor = possessor;
            try
            {
                possessor.OnPossessed(this);
            }
            catch
            {
                // A possessor that refuses the body must not leave the pawn believing it is driven.
                CurrentPossessor = null;
                throw;
            }

            Possessed?.Invoke(this, possessor);
            return PossessionResult.Success;
        }

        /// <summary>
        /// Hands the pawn to <paramref name="possessor"/>, releasing whoever holds it first.
        /// </summary>
        /// <param name="possessor">The driver taking the pawn.</param>
        /// <returns>
        /// The outcome. Never returns <see cref="PossessionResult.AlreadyPossessed"/> — that is the
        /// case this method exists to handle — but still refuses an unspawned or dead pawn.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="possessor"/> is <c>null</c>.</exception>
        public PossessionResult Possess(IPawnPossessor possessor)
        {
            if (possessor == null) throw new ArgumentNullException(nameof(possessor));

            if (ReferenceEquals(CurrentPossessor, possessor)) return PossessionResult.AlreadyOwner;
            if (State == PawnState.Unspawned || State == PawnState.Despawned) return PossessionResult.NotSpawned;
            if (State == PawnState.Dead) return PossessionResult.PawnDead;
            if (State == PawnState.Incapacitated) return PossessionResult.PawnIncapacitated;

            ReleaseInternal();
            return TryPossess(possessor);
        }

        /// <summary>
        /// Gives up the current possessor, raising <see cref="IPawnPossessor.OnReleased"/> and
        /// <see cref="Released"/>. No-ops when nothing holds the pawn.
        /// </summary>
        public void Release() => ReleaseInternal();

        /// <summary>
        /// Runs <paramref name="damage"/> through the modifier pipeline, removes what is left from
        /// the pawn's vitals, and kills the pawn when they reach zero.
        /// </summary>
        /// <param name="damage">The incoming damage event.</param>
        /// <returns>How much was actually removed. Zero when the pawn cannot take damage right now, the amount was not positive, the pawn is invulnerable, or modifiers absorbed it all.</returns>
        /// <remarks>
        /// Order: state check, positive-amount check, invulnerability check, definition modifiers
        /// top to bottom, then <see cref="IDamageModifier"/> components in component order, then the
        /// vitals change, then <see cref="DamageTaken"/>, then <see cref="Died"/> or
        /// <see cref="Incapacitated"/> if this was fatal. A hit absorbed to zero still raises
        /// <see cref="DamageTaken"/> so hit reactions and "blocked!" feedback still fire.
        /// <para>
        /// A pawn that is already <see cref="PawnState.Incapacitated"/> takes a shortcut: any
        /// qualifying hit (respecting invulnerability, skipping the modifier pipeline — armor does
        /// not save a downed target) finishes it straight to <see cref="PawnState.Dead"/>. Only a
        /// pawn whose <see cref="PawnDefinition.CanBeIncapacitated"/> is set ever reaches
        /// <see cref="PawnState.Incapacitated"/> in the first place; everyone else dies on the
        /// first fatal blow exactly as before.
        /// </para>
        /// </remarks>
        public float ApplyDamage(in DamageInfo damage)
        {
            if (State == PawnState.Incapacitated)
            {
                if (damage.Amount <= 0f) return 0f;
                if (IsInvulnerable && !damage.IgnoresInvulnerability) return 0f;

                DamageTaken?.Invoke(this, damage);
                Die(DeathInfo.FromDamage(damage));
                return 0f;
            }

            if (!IsAlive) return 0f;
            if (damage.Amount <= 0f) return 0f;
            if (IsInvulnerable && !damage.IgnoresInvulnerability) return 0f;

            DamageInfo modified = RunModifiers(damage);
            if (modified.Amount <= 0f)
            {
                DamageTaken?.Invoke(this, modified.WithAmount(0f));
                return 0f;
            }

            applyingDamage = true;
            float applied = -Vitals.ApplyDelta(-modified.Amount);
            applyingDamage = false;

            DamageTaken?.Invoke(this, modified);

            if (Vitals.IsDepleted)
            {
                DeathInfo info = DeathInfo.FromDamage(modified, modified.Amount - applied);
                if (definition != null && definition.CanBeIncapacitated) Incapacitate(info);
                else Die(info);
            }

            return applied;
        }

        /// <summary>Restores vitals to a living pawn.</summary>
        /// <param name="amount">How much to restore. Zero or negative values are ignored.</param>
        /// <returns>How much was actually restored after clamping at the maximum.</returns>
        /// <remarks>
        /// Healing a dead or incapacitated pawn does nothing — deliberately, so a stray
        /// area-of-effect heal can never resurrect a corpse or pick a downed teammate back up.
        /// Recovery is <see cref="TryRevive"/> and nothing else.
        /// </remarks>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            return Vitals.ApplyDelta(amount);
        }

        /// <summary>
        /// Kills the pawn outright, ignoring invulnerability and the damage pipeline.
        /// </summary>
        /// <param name="info">What to record as the cause; defaults to an empty record.</param>
        /// <remarks>
        /// For scripted deaths, execution moves and falling out of the world. Drains vitals to zero
        /// first, so anything watching <see cref="IVitalSource.Changed"/> sees a consistent pawn.
        /// Always goes straight to <see cref="PawnState.Dead"/> — unlike <see cref="ApplyDamage"/>,
        /// <c>Kill</c> never routes through <see cref="PawnState.Incapacitated"/>, whether it is
        /// called on a living pawn or to finish off one already downed. No-ops unless the pawn is
        /// alive or incapacitated.
        /// </remarks>
        public void Kill(DeathInfo info = default)
        {
            if (State != PawnState.Alive && State != PawnState.Incapacitated) return;

            if (State == PawnState.Alive)
            {
                applyingDamage = true;
                Vitals.ApplyDelta(-Vitals.Current);
                applyingDamage = false;
            }

            Die(info);
        }

        /// <summary>
        /// Brings a dead or incapacitated pawn back with part or all of its vitals and a fresh
        /// grace period.
        /// </summary>
        /// <param name="info">How much to restore, who did it, and how long the grace period lasts.</param>
        /// <returns>
        /// <see cref="ReviveResult.Success"/>, <see cref="ReviveResult.NotDead"/> for a pawn that is
        /// neither dead nor incapacitated, or <see cref="ReviveResult.NotSpawned"/> for one that is
        /// not in play. Both failures change nothing.
        /// </returns>
        /// <remarks>
        /// One method recovers both a corpse and a downed pawn — from the pawn's own perspective
        /// "come back to <see cref="PawnState.Alive"/>" is the same operation either way, so there
        /// is no separate "pick up a downed teammate" API to learn.
        /// </remarks>
        public ReviveResult TryRevive(ReviveInfo info)
        {
            if (State == PawnState.Unspawned || State == PawnState.Despawned) return ReviveResult.NotSpawned;
            if (State != PawnState.Dead && State != PawnState.Incapacitated) return ReviveResult.NotDead;

            float fraction = info.VitalFraction <= 0f ? 1f : Mathf.Clamp01(info.VitalFraction);
            Vitals.Fill(fraction);

            State = PawnState.Alive;
            incapacitationRemaining = 0f;

            float grace = info.InvulnerabilityDuration < 0f
                ? (definition != null ? definition.SpawnInvulnerability : 0f)
                : info.InvulnerabilityDuration;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = Mathf.Max(0f, grace);

            Revived?.Invoke(this);
            return ReviveResult.Success;
        }

        /// <summary>Turns indefinite invulnerability on or off, independently of any timed grace period.</summary>
        /// <param name="value"><c>true</c> to ignore damage until turned off again.</param>
        public void SetInvulnerable(bool value) => invulnerableIndefinitely = value;

        /// <summary>Grants invulnerability for <paramref name="duration"/> seconds of ticked time.</summary>
        /// <param name="duration">How long the grace period lasts. Values at or below zero clear it.</param>
        /// <remarks>Counted down by <see cref="Tick"/>, so a paused game does not burn through it.</remarks>
        public void SetInvulnerable(float duration) => invulnerabilityRemaining = Mathf.Max(0f, duration);

        /// <summary>
        /// Advances this pawn's timed state by <paramref name="amount"/>. Call once per frame in a
        /// real-time game, or once per round in a turn-based one.
        /// </summary>
        /// <param name="amount">Elapsed time — seconds, turns, whatever the game's clock counts.</param>
        /// <remarks>
        /// The pawn has no <c>Update</c> of its own so the host stays in charge of time. This
        /// advances the invulnerability grace period and, for a downed pawn with a timed
        /// <see cref="PawnDefinition.IncapacitationDuration"/>, the bleed-out clock — when it
        /// reaches zero the pawn dies, carrying the cause of the original incapacitating blow.
        /// Possessors and capabilities tick themselves.
        /// </remarks>
        public void Tick(float amount)
        {
            if (State == PawnState.Incapacitated && incapacitationRemaining > 0f)
            {
                incapacitationRemaining = Mathf.Max(0f, incapacitationRemaining - amount);
                if (incapacitationRemaining <= 0f)
                {
                    Die(incapacitationCause);
                    return;
                }
            }

            if (invulnerabilityRemaining <= 0f) return;
            invulnerabilityRemaining = Mathf.Max(0f, invulnerabilityRemaining - amount);
        }

        /// <summary>
        /// Looks up a capability on this pawn — <c>IPawnMotor</c>, or anything the game has
        /// composed onto the GameObject.
        /// </summary>
        /// <typeparam name="T">The capability interface to ask for.</typeparam>
        /// <param name="capability">The capability, or <c>null</c> when this pawn does not have it.</param>
        /// <returns><c>true</c> when the capability was found.</returns>
        /// <remarks>
        /// Checks the pawn itself, then a motor supplied through <see cref="SetMotor"/>, then
        /// components on this GameObject and its children, including inactive ones.
        /// Deliberately the same shape as <c>ICapabilityProvider.TryGet</c> in the
        /// Eldritch Input System and <c>IInteractable.TryGet</c> in the Interaction System, so a
        /// command written against one works against all three.
        /// <code>
        /// if (pawn.TryGet&lt;IPawnMotor&gt;(out var motor)) motor.Move(direction * speed, deltaTime);
        /// </code>
        /// A game adds a capability by adding a component — never by subclassing <see cref="Pawn"/>,
        /// which is sealed.
        /// </remarks>
        public bool TryGet<T>(out T capability) where T : class
        {
            capability = this as T;
            if (capability != null) return true;

            // A motor injected with SetMotor is not a component, so it has to be offered explicitly.
            capability = motor as T;
            if (capability != null) return true;

            capability = GetComponentInChildren<T>(true);
            return capability != null;
        }

        /// <summary>Returns how this pawn regards <paramref name="other"/>, via <see cref="TeamResolver"/>.</summary>
        /// <param name="other">The pawn being regarded.</param>
        public PawnRelationship RelationshipTo(Pawn other) => teamResolver.Resolve(this, other);

        /// <summary>Shorthand for a <see cref="PawnRelationship.Hostile"/> relationship.</summary>
        /// <param name="other">The pawn being regarded.</param>
        public bool IsHostileTo(Pawn other) => RelationshipTo(other) == PawnRelationship.Hostile;

        /// <summary>Shorthand for a <see cref="PawnRelationship.Friendly"/> relationship.</summary>
        /// <param name="other">The pawn being regarded.</param>
        public bool IsFriendlyTo(Pawn other) => RelationshipTo(other) == PawnRelationship.Friendly;

        /// <summary>
        /// Replaces the pawn's vitals — for an ability-system-backed pool, a shared squad health
        /// bar, or a test double.
        /// </summary>
        /// <param name="source">The vital source to use from now on.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Call before <see cref="Spawn()"/> — typically from <c>Awake</c>. The pawn watches
        /// <see cref="IVitalSource.Changed"/> on whatever source it holds, so a pool drained by
        /// something other than <see cref="ApplyDamage"/> still kills the pawn.
        /// </remarks>
        public void SetVitalSource(IVitalSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (vitals != null) vitals.Changed -= OnVitalsChanged;
            vitals = source;
            vitals.Changed += OnVitalsChanged;
        }

        /// <summary>Replaces the pawn's motor, bypassing the component search.</summary>
        /// <param name="value">The motor to use from now on, or <c>null</c> to clear it.</param>
        /// <remarks>Useful for possession-driven movement rigs and for EditMode tests that use a fake motor.</remarks>
        public void SetMotor(IPawnMotor value) => motor = value;

        /// <summary>
        /// Re-reads the <see cref="IDamageModifier"/> components on this GameObject. Call after
        /// adding or removing one at runtime.
        /// </summary>
        /// <remarks>
        /// The list is cached at spawn so the damage pipeline allocates nothing per hit. Modifiers
        /// authored on the <see cref="PawnDefinition"/> always run before component modifiers.
        /// </remarks>
        public void RefreshComponentModifiers()
        {
            componentModifiers.Clear();
            GetComponents(componentModifiers);
        }

        private void Start()
        {
            if (spawnOnStart && State == PawnState.Unspawned) Spawn();
        }

        private void OnDestroy()
        {
            ReleaseInternal();
            if (vitals != null) vitals.Changed -= OnVitalsChanged;
        }

        private IVitalSource BuildVitals()
        {
            if (definition == null)
                throw new InvalidOperationException(
                    $"Pawn '{name}' has no PawnDefinition and no vital source. Assign a definition in the inspector, or call SetVitalSource before using Vitals.");

            SetVitalSource(new Health(definition.MaxVital));
            return vitals;
        }

        private DamageInfo RunModifiers(in DamageInfo damage)
        {
            DamageInfo current = damage;

            if (definition != null)
            {
                IReadOnlyList<IDamageModifier> authored = definition.DamageModifiers;
                for (int i = 0; i < authored.Count; i++)
                {
                    IDamageModifier modifier = authored[i];
                    if (modifier != null) current = modifier.Modify(this, current);
                }
            }

            for (int i = 0; i < componentModifiers.Count; i++)
            {
                IDamageModifier modifier = componentModifiers[i];
                if (modifier != null) current = modifier.Modify(this, current);
            }

            return current;
        }

        private void Die(in DeathInfo info)
        {
            if (State != PawnState.Alive && State != PawnState.Incapacitated) return;

            State = PawnState.Dead;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = 0f;
            incapacitationRemaining = 0f;

            if (definition == null || definition.ReleaseOnDeath) ReleaseInternal();

            Died?.Invoke(this, info);
        }

        /// <summary>
        /// Downs the pawn straight to <see cref="PawnState.Incapacitated"/>, no-op unless it is
        /// currently <see cref="PawnState.Alive"/>. Internal because the only callers are
        /// <see cref="ApplyDamage"/>, <see cref="OnVitalsChanged"/>, and
        /// <see cref="Persistence.PawnPersistence.RestoreState"/> restoring a saved incapacitated
        /// pawn — everything else reaches this state through those, never directly.
        /// </summary>
        /// <param name="info">What to record as the cause.</param>
        internal void Incapacitate(in DeathInfo info)
        {
            if (State != PawnState.Alive) return;

            State = PawnState.Incapacitated;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = 0f;
            incapacitationCause = info;
            incapacitationRemaining = definition != null ? Mathf.Max(0f, definition.IncapacitationDuration) : 0f;

            if (definition != null && definition.ReleaseOnIncapacitation) ReleaseInternal();

            Incapacitated?.Invoke(this, info);
        }

        private void ReleaseInternal()
        {
            IPawnPossessor previous = CurrentPossessor;
            if (previous == null) return;

            CurrentPossessor = null;
            previous.OnReleased(this);
            Released?.Invoke(this, previous);
        }

        private void OnVitalsChanged(float current, float max)
        {
            if (applyingDamage) return;
            if (State != PawnState.Alive) return;
            if (current > 0f) return;

            if (definition != null && definition.CanBeIncapacitated) Incapacitate(default);
            else Die(default);
        }
    }
}
