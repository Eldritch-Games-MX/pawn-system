using System;
using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// A world object with an identity and a lifecycle — spawn, damage, death, incapacitation,
    /// revival, despawn — but no possessor. A destructible crate, a turret, a door: anything that
    /// breaks or is destroyed without ever being driven by a player or an AI.
    /// </summary>
    /// <remarks>
    /// See <see cref="Pawn"/> for the possessable counterpart. The two deliberately share the same
    /// lifecycle contract — state machine, damage pipeline, incapacitation, revival — so a damage
    /// modifier, a spawner or a save system can treat them alike wherever possession does not
    /// matter, while a destructible prop never inherits <see cref="Pawn"/>'s possession, movement or
    /// view-point API it has no use for.
    /// <code>
    /// var crate = Instantiate(cratePrefab).GetComponent&lt;Actor&gt;();
    /// crate.Spawn();
    /// crate.ApplyDamage(new DamageInfo(25f, fireType, instigator: player));
    /// </code>
    /// <para>
    /// One component, sealed like <see cref="Pawn"/> — add behaviour through a capability component
    /// reached via <see cref="TryGet{T}"/>, never by subclassing this one.
    /// </para>
    /// <para>
    /// Deliberately its own implementation rather than sharing code with <see cref="Pawn"/> behind a
    /// common internal base: the two are peers with only the lifecycle in common, <see cref="Pawn"/>
    /// is sealed on purpose, and a delegation layer would cost more than the modest amount of logic
    /// it would save. Keep the two lifecycles in sync by hand when the shared contract changes —
    /// both are covered by the same shape of test suite for exactly that reason.
    /// </para>
    /// </remarks>
    public sealed class Actor : MonoBehaviour, IDamageable, IDamageTarget
    {
        [Header("Bindings")]
        [Tooltip("The authored recipe for this actor: identity, vitals and death behaviour.")]
        [SerializeField] private ActorDefinition definition;

        [Header("Configuration")]
        [Tooltip("Spawn in place on Start when nothing has spawned this actor yet. Turn off for actors a spawner or pool owns.")]
        [SerializeField] private bool spawnOnStart = true;

        private readonly PawnTagContainer tags = new PawnTagContainer();
        private readonly List<IDamageModifier> componentModifiers = new List<IDamageModifier>();

        private IVitalSource vitals;
        private float invulnerabilityRemaining;
        private bool invulnerableIndefinitely;
        private bool applyingDamage;
        private float incapacitationRemaining;
        private DeathInfo incapacitationCause;

        /// <summary>The authored recipe this actor was built from, or <c>null</c> when none is assigned.</summary>
        public ActorDefinition Definition => definition;

        /// <summary>Where this actor is in its lifecycle. Changed only through the methods on this class.</summary>
        public PawnState State { get; private set; } = PawnState.Unspawned;

        /// <summary><c>true</c> when the actor is in play and not destroyed — the only state in which it can be hurt.</summary>
        public bool IsAlive => State == PawnState.Alive;

        /// <summary><c>true</c> when the actor is downed but not destroyed. See <see cref="PawnState.Incapacitated"/>.</summary>
        public bool IsIncapacitated => State == PawnState.Incapacitated;

        /// <summary>The actor's runtime tags, seeded from <see cref="ActorDefinition.Tags"/> when it spawns.</summary>
        public PawnTagContainer Tags => tags;

        /// <summary>
        /// The pool this actor is destroyed when it runs out of. Built from
        /// <see cref="ActorDefinition.MaxVital"/> on first use unless <see cref="SetVitalSource"/>
        /// replaced it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No vital source was assigned and there is no <see cref="ActorDefinition"/> to build one from.
        /// </exception>
        public IVitalSource Vitals => vitals ?? BuildVitals();

        // IDamageTarget is deliberately explicit — see the identical note on Pawn.
        Transform IDamageTarget.Transform => transform;
        IVitalSource IDamageTarget.Vitals => Vitals;

        /// <summary>
        /// <c>true</c> while damage is being ignored — during a spawn or revival grace period, or
        /// because <see cref="SetInvulnerable(bool)"/> was set.
        /// </summary>
        /// <remarks>
        /// Invulnerability never protects against <see cref="Kill"/>, nor against damage whose
        /// <see cref="DamageTypeDefinition.IgnoresInvulnerability"/> is set.
        /// </remarks>
        public bool IsInvulnerable => invulnerableIndefinitely || invulnerabilityRemaining > 0f;

        /// <summary>Seconds of grace remaining, or zero when the actor is not on a timed invulnerability.</summary>
        public float InvulnerabilityRemaining => invulnerabilityRemaining;

        /// <summary>
        /// Seconds before a downed actor collapses on its own, or zero when the actor is not
        /// incapacitated or has no timer. Drive a countdown UI bar from this.
        /// </summary>
        public float IncapacitationRemaining => incapacitationRemaining;

        /// <summary>Raised after the actor enters play and is <see cref="PawnState.Alive"/>.</summary>
        public event Action<Actor> Spawned;

        /// <summary>Raised after the actor leaves play and is <see cref="PawnState.Despawned"/>.</summary>
        public event Action<Actor> Despawned;

        /// <summary>Raised after a destroyed actor is brought back and is <see cref="PawnState.Alive"/> again.</summary>
        public event Action<Actor> Revived;

        /// <summary>
        /// Raised for every hit that reaches the actor, carrying the damage <em>after</em> the
        /// modifier pipeline — including fully absorbed hits, which arrive with an amount of zero.
        /// </summary>
        public event Action<Actor, DamageInfo> DamageTaken;

        /// <summary>
        /// Raised after the actor is destroyed, with the record of what did it. Fires exactly once
        /// per death; a revived actor that is destroyed again fires it again.
        /// </summary>
        public event Action<Actor, DeathInfo> Died;

        /// <summary>
        /// Raised when a fatal blow downs an incapacitation-capable actor instead of destroying it —
        /// see <see cref="PawnState.Incapacitated"/>. The actor is still addressable;
        /// <see cref="TryRevive"/> also recovers an incapacitated actor back to
        /// <see cref="PawnState.Alive"/>.
        /// </summary>
        public event Action<Actor, DeathInfo> Incapacitated;

        /// <summary>Spawns the actor where it already stands.</summary>
        /// <returns>The outcome; see <see cref="SpawnResult"/>.</returns>
        public SpawnResult Spawn() => Spawn(transform.position, transform.rotation);

        /// <summary>
        /// Brings the actor into play at <paramref name="position"/>: reactivates the GameObject,
        /// resets tags from the definition, fills vitals, grants the spawn grace period, and raises
        /// <see cref="Spawned"/>.
        /// </summary>
        /// <param name="position">Where to place the actor.</param>
        /// <param name="rotation">Which way to face it.</param>
        /// <returns>
        /// <see cref="SpawnResult.Success"/>, <see cref="SpawnResult.AlreadySpawned"/> when the actor
        /// is already in play, or <see cref="SpawnResult.MissingDefinition"/> when it has neither a
        /// definition nor an injected vital source. Both failures change nothing.
        /// </returns>
        /// <remarks>
        /// No motor seam like <see cref="Pawn"/>'s — an actor is never driven, so placement always
        /// just sets the transform directly.
        /// </remarks>
        public SpawnResult Spawn(Vector3 position, Quaternion rotation)
        {
            if (State == PawnState.Alive || State == PawnState.Dead || State == PawnState.Incapacitated) return SpawnResult.AlreadySpawned;
            if (definition == null && vitals == null) return SpawnResult.MissingDefinition;

            gameObject.SetActive(true);
            transform.SetPositionAndRotation(position, rotation);

            tags.Clear();
            if (definition != null)
            {
                IReadOnlyList<PawnTag> authored = definition.Tags;
                for (int i = 0; i < authored.Count; i++) tags.Add(authored[i]);
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
        /// Takes the actor out of play: deactivates the GameObject when the definition says to, and
        /// raises <see cref="Despawned"/>.
        /// </summary>
        /// <remarks>
        /// Despawning never destroys anything — destruction or pooling is the caller's decision, so
        /// a pooled actor can be spawned again later. No-ops when the actor is already despawned.
        /// </remarks>
        public void Despawn()
        {
            if (State == PawnState.Despawned) return;

            State = PawnState.Despawned;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = 0f;
            incapacitationRemaining = 0f;

            if (definition == null || definition.DeactivateOnDespawn) gameObject.SetActive(false);

            Despawned?.Invoke(this);
        }

        /// <summary>
        /// Runs <paramref name="damage"/> through the modifier pipeline, removes what is left from
        /// the actor's vitals, and destroys the actor when they reach zero.
        /// </summary>
        /// <param name="damage">The incoming damage event.</param>
        /// <returns>How much was actually removed. Zero when the actor cannot take damage right now, the amount was not positive, the actor is invulnerable, or modifiers absorbed it all.</returns>
        /// <remarks>
        /// Identical contract to <see cref="Pawn.ApplyDamage"/>: definition modifiers, then component
        /// modifiers, then the vitals change, then <see cref="DamageTaken"/>, then <see cref="Died"/>
        /// or <see cref="Incapacitated"/> if this was fatal. An already-incapacitated actor skips the
        /// modifier pipeline entirely — armor does not save something already down.
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

        /// <summary>Restores vitals to a living actor.</summary>
        /// <param name="amount">How much to restore. Zero or negative values are ignored.</param>
        /// <returns>How much was actually restored after clamping at the maximum.</returns>
        /// <remarks>
        /// Healing a destroyed or incapacitated actor does nothing, for the same reason as
        /// <see cref="Pawn.Heal"/>: recovery is <see cref="TryRevive"/> and nothing else.
        /// </remarks>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            return Vitals.ApplyDelta(amount);
        }

        /// <summary>Destroys the actor outright, ignoring invulnerability and the damage pipeline.</summary>
        /// <param name="info">What to record as the cause; defaults to an empty record.</param>
        /// <remarks>
        /// Identical contract to <see cref="Pawn.Kill"/>: always goes straight to
        /// <see cref="PawnState.Dead"/>, whether called on a living actor or to finish one already
        /// downed. No-ops unless the actor is alive or incapacitated.
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
        /// Brings a destroyed or incapacitated actor back with part or all of its vitals and a fresh
        /// grace period.
        /// </summary>
        /// <param name="info">How much to restore, who did it, and how long the grace period lasts.</param>
        /// <returns>
        /// <see cref="ReviveResult.Success"/>, <see cref="ReviveResult.NotDead"/> for an actor that
        /// is neither destroyed nor incapacitated, or <see cref="ReviveResult.NotSpawned"/> for one
        /// that is not in play. Both failures change nothing.
        /// </returns>
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

        /// <summary>
        /// Downs a living actor without touching its vitals: <see cref="PawnState.Alive"/> becomes
        /// <see cref="PawnState.Incapacitated"/>.
        /// </summary>
        /// <param name="info">What to record as the cause; defaults to an empty record.</param>
        /// <returns><c>true</c> if the actor was alive and is now incapacitated; <c>false</c> (and nothing raised) in every other state.</returns>
        /// <remarks>
        /// Identical contract to <see cref="Pawn.TryIncapacitate"/> — for a knock-out that is not
        /// damage (a thrown object, a collapsing shelf). Works regardless of
        /// <see cref="ActorDefinition.CanBeIncapacitated"/>, which only decides what a fatal blow does.
        /// </remarks>
        public bool TryIncapacitate(DeathInfo info = default)
        {
            if (State != PawnState.Alive) return false;
            Incapacitate(info);
            return true;
        }

        /// <summary>Turns indefinite invulnerability on or off, independently of any timed grace period.</summary>
        /// <param name="value"><c>true</c> to ignore damage until turned off again.</param>
        public void SetInvulnerable(bool value) => invulnerableIndefinitely = value;

        /// <summary>Grants invulnerability for <paramref name="duration"/> seconds of ticked time.</summary>
        /// <param name="duration">How long the grace period lasts. Values at or below zero clear it.</param>
        /// <remarks>Counted down by <see cref="Tick"/>, so a paused game does not burn through it.</remarks>
        public void SetInvulnerable(float duration) => invulnerabilityRemaining = Mathf.Max(0f, duration);

        /// <summary>
        /// Advances this actor's timed state by <paramref name="amount"/>. Call once per frame in a
        /// real-time game, or once per round in a turn-based one.
        /// </summary>
        /// <param name="amount">Elapsed time — seconds, turns, whatever the game's clock counts.</param>
        /// <remarks>
        /// The actor has no <c>Update</c> of its own. Advances the invulnerability grace period and,
        /// for a downed actor with a timed <see cref="ActorDefinition.IncapacitationDuration"/>, the
        /// collapse clock — when it reaches zero the actor is destroyed, carrying the cause of the
        /// original incapacitating blow.
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
        /// Looks up a capability on this actor — anything the game has composed onto the GameObject.
        /// </summary>
        /// <typeparam name="T">The capability interface to ask for.</typeparam>
        /// <param name="capability">The capability, or <c>null</c> when this actor does not have it.</param>
        /// <returns><c>true</c> when the capability was found.</returns>
        /// <remarks>
        /// Same shape as <see cref="Pawn.TryGet{T}"/>: checks the actor itself, then components on
        /// this GameObject and its children, including inactive ones. A game adds a capability by
        /// adding a component — never by subclassing <see cref="Actor"/>, which is sealed.
        /// </remarks>
        public bool TryGet<T>(out T capability) where T : class
        {
            capability = this as T;
            if (capability != null) return true;

            capability = GetComponentInChildren<T>(true);
            return capability != null;
        }

        /// <summary>
        /// Replaces the actor's vitals — for a shared health pool or a test double.
        /// </summary>
        /// <param name="source">The vital source to use from now on.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <remarks>Call before <see cref="Spawn()"/> — typically from <c>Awake</c>.</remarks>
        public void SetVitalSource(IVitalSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (vitals != null) vitals.Changed -= OnVitalsChanged;
            vitals = source;
            vitals.Changed += OnVitalsChanged;
        }

        /// <summary>
        /// Re-reads the <see cref="IDamageModifier"/> components on this GameObject. Call after
        /// adding or removing one at runtime.
        /// </summary>
        /// <remarks>
        /// The list is cached at spawn so the damage pipeline allocates nothing per hit. Modifiers
        /// authored on the <see cref="ActorDefinition"/> always run before component modifiers.
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
            if (vitals != null) vitals.Changed -= OnVitalsChanged;
        }

        private IVitalSource BuildVitals()
        {
            if (definition == null)
                throw new InvalidOperationException(
                    $"Actor '{name}' has no ActorDefinition and no vital source. Assign a definition in the inspector, or call SetVitalSource before using Vitals.");

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

            Died?.Invoke(this, info);
        }

        private void Incapacitate(in DeathInfo info)
        {
            if (State != PawnState.Alive) return;

            State = PawnState.Incapacitated;
            invulnerableIndefinitely = false;
            invulnerabilityRemaining = 0f;
            incapacitationCause = info;
            incapacitationRemaining = definition != null ? Mathf.Max(0f, definition.IncapacitationDuration) : 0f;

            Incapacitated?.Invoke(this, info);
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
