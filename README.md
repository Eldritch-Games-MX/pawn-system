# Eldritch Games — Pawn System

A genre-agnostic pawn framework for Unity. A *pawn* is any character in the
world — player-driven or AI-driven — with an identity, vitals, a movement seam,
and a lifecycle covering spawn, death, revival, possession and release. One
`Pawn` component and a handful of interfaces cover a turn-based RPG, a horror
game and a shooter, without the core package knowing anything about any of them.

Possession is symmetric, the way Unreal models it: a player controller and an AI
brain are both `IPawnPossessor`, and the pawn cannot tell which one is driving
it. That is what makes a body swappable mid-game.

The core has **no package dependencies** — not on input, not on abilities, not on
interaction. Two optional adapter assemblies bridge it to the Eldritch Input
System (possession by a player) and the Eldritch Ability System (vitals backed by
an attribute), and each drops out of the build when its package is absent.

See `Documentation~/index.md` for the full guide, `Documentation~/architecture.md`
for how the pieces fit together, `Documentation~/pawn-taxonomy.md` for how common
character shapes (player character, NPC, boss, turret, vehicle, board piece) map
onto the system, and `Documentation~/extending-pawns.md` for how to add
capabilities in a downstream project without modifying this package.

## Quick start

1. Create a `PawnDefinition` asset (**Assets > Create > Eldritch Games > Pawn
   System > Pawn Definition**) and set its Id, Max Vital and — optionally — a
   `TeamDefinition`, tags and a damage-modifier pipeline.
2. Add a `Pawn` component to your character prefab and assign the definition.
   Add whatever capabilities the character needs as separate components — a
   `CharacterControllerMotor` for movement, your own components for everything
   else.
3. Bring it into play with `pawn.Spawn(position, rotation)`, or let a
   `PawnSpawner` do it and handle respawning.
4. Give it a driver with `pawn.TryPossess(possessor)` — a `ControllerPossessor`
   wrapping an Eldritch Input System `IController` for a player, or your own
   `IPawnPossessor` for an AI. `pawn.Release()` hands the body back.
5. Hurt it with `pawn.ApplyDamage(new DamageInfo(25f, fireType, instigator))`,
   bring it back with `pawn.TryRevive(ReviveInfo.Full)`.
6. Drive time by calling `pawn.Tick(Time.deltaTime)` each frame in a real-time
   game, or `pawn.Tick(1f)` per round in a turn-based one. The package has no
   `Update` of its own.

## Examples

### Spawn, possess, release

```csharp
Pawn pawn = Instantiate(knightPrefab).GetComponent<Pawn>();
pawn.Spawn(spawnPoint.position, spawnPoint.rotation);

var possessor = new ControllerPossessor(playerController);   // Input System adapter
pawn.TryPossess(possessor);          // Success / AlreadyPossessed / NotSpawned / PawnDead

// hand the body to an AI instead, in one call
pawn.Possess(new WanderingBrain());  // releases the previous possessor automatically

pawn.Release();                      // nothing drives the pawn now
```

### Damage, death, revive

```csharp
DamageTypeDefinition fire = ...;

float applied = pawn.ApplyDamage(new DamageInfo(25f, fire, instigator: dragon));

pawn.Died += (dead, info) =>
{
    Debug.Log($"{dead.name} killed by {info.Instigator} (overkill {info.Overkill})");
};

pawn.Heal(10f);                        // no-op on a dead pawn — healing never revives
pawn.TryRevive(new ReviveInfo(0.5f));  // back at half vitals, with a fresh grace period
```

### Teams and relationships

```csharp
public bool IsThreat(Pawn subject, Pawn other) =>
    subject.RelationshipTo(other) == PawnRelationship.Hostile;

// swap sides at runtime — mind control, a defection
mindControlledGoblin.Team = playerTeam;

// or replace the whole resolution policy
pawn.TeamResolver = new ReputationBasedResolver(reputationSystem);
```

### Capabilities, not inheritance

```csharp
public interface IClimbable { void Climb(Ledge ledge); }

public sealed class Climber : MonoBehaviour, IClimbable
{
    public void Climb(Ledge ledge) { /* ... */ }
}

// anywhere holding a Pawn reference
if (pawn.TryGet<IClimbable>(out var climber))
    climber.Climb(nearestLedge);
```

### Spawning and respawning a population

```csharp
spawner.Prefab = gruntPrefab;
spawner.SpawnPointProvider = new TransformSpawnPointProvider(arenaCorners);
spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 5f, maxRespawns: 3);
spawner.Pool = new SimplePawnPool(poolRoot);

Pawn grunt = spawner.Spawn();

void Update() => spawner.Tick(Time.deltaTime);   // advances pending respawns

// allocation-free population queries
var buffer = new List<Pawn>();
spawner.Registry.Query(buffer, team: monsters, state: PawnState.Alive);
Pawn nearest = spawner.Registry.FindNearest(player.transform.position, p => p.IsHostileTo(player));
```

### Save and load

```csharp
PawnSaveData data = pawn.CaptureState();
string json = JsonUtility.ToJson(data);
// ... later ...
pawn.RestoreState(JsonUtility.FromJson<PawnSaveData>(json), teamLookup);
```

### Ability System adapter (optional)

```csharp
// keeps a pawn's vitals and an AbilitySystemComponent attribute as one number
pawn.SetVitalSource(new AttributeVitalSource(abilitySystemComponent, healthAttribute));
// or wire it from the inspector with AbilityVitalsBinder
```

## API reference

The full surface is documented with XML comments in the source (IntelliSense/
IDE tooltips) and in `Documentation~/architecture.md`; this is the map.

### `Pawn` (sealed MonoBehaviour, the only required component)

| Member | Description |
|---|---|
| `Definition` | The `PawnDefinition` this pawn was built from |
| `State` | `Unspawned` / `Alive` / `Dead` / `Despawned` |
| `IsAlive` | Shorthand for `State == Alive` |
| `Tags` | Runtime `PawnTagContainer`, seeded from the definition |
| `Team` | Current `TeamDefinition`, freely reassignable |
| `TeamResolver` | `ITeamResolver` used by `RelationshipTo` |
| `ViewPoint` | Eyes/aim `Transform`, falls back to the pawn's own transform |
| `Vitals` | The `IVitalSource` backing health/hull/sanity/etc. |
| `Motor` | The `IPawnMotor` found on this GameObject, or `null` |
| `CurrentPossessor` / `IsPossessed` | Whoever is currently driving the pawn |
| `IsInvulnerable` / `InvulnerabilityRemaining` | Damage-immunity state |
| `Spawn()` / `Spawn(position, rotation)` | → `SpawnResult` |
| `Despawn()` | Takes the pawn out of play (releases possessor, deactivates GameObject) |
| `TryPossess(possessor)` | → `PossessionResult`; fails if already possessed |
| `Possess(possessor)` | Swaps possessor, releasing the previous one first |
| `Release()` | Clears the current possessor |
| `ApplyDamage(in DamageInfo)` | → amount actually applied, after modifiers/invulnerability |
| `Heal(amount)` | No-op while dead |
| `Kill(DeathInfo)` | Forces death, bypassing invulnerability |
| `TryRevive(ReviveInfo)` | → `ReviveResult`; only way to bring a dead pawn back |
| `SetInvulnerable(bool)` / `SetInvulnerable(duration)` | Indefinite or timed immunity |
| `Tick(amount)` | Advances timed state (invulnerability, etc.) — call once per frame or round |
| `TryGet<T>(out capability)` | Capability lookup, same shape as the Input/Interaction systems |
| `RelationshipTo` / `IsHostileTo` / `IsFriendlyTo` | Team relationship queries |
| `SetVitalSource(IVitalSource)` | Replaces the vitals backing (e.g. `AttributeVitalSource`) |
| `SetMotor(IPawnMotor)` | Overrides motor discovery |
| `RefreshComponentModifiers()` | Re-scans `IDamageModifier` components after adding/removing one |
| `Spawned`, `Despawned`, `Revived`, `DamageTaken`, `Died`, `Possessed`, `Released` | Events |

### Identity

| Type | Purpose |
|---|---|
| `PawnDefinition` (SO) | Authored id, display name, icon, team, tags, max vital, damage modifiers |
| `PawnTag` / `PawnTagContainer` | Dotted hierarchical tags (`"Status.Stunned"`) |
| `TeamDefinition` (SO) | A side, with hostile/friendly team lists |
| `ITeamResolver` / `DefaultTeamResolver` | Resolves `PawnRelationship` (Friendly/Neutral/Hostile) between two pawns |

### Vitals

| Type | Purpose |
|---|---|
| `IVitalSource` / `Health` | The seam for health/hull/sanity/etc., and its default implementation |
| `DamageInfo` | Immutable damage event: amount, type, instigator, source, body part, hit point |
| `DamageTypeDefinition` (SO) | An authored damage kind, replacing a closed enum |
| `IDamageModifier` / `FlatDamageReduction` | Pipeline step for armor/resistance, and its built-in |
| `DamageReceiver` | Per-collider hit zone with a multiplier and body-part tag |
| `DeathInfo` / `ReviveInfo` | Structured death/revive payloads |
| `IDamageable` | Implemented by `Pawn`; targetable by weapons/traps generically |

### Movement

| Type | Purpose |
|---|---|
| `IPawnMotor` | Move/Look/Teleport/Stop contract — entirely optional |
| `CharacterControllerMotor` | Built-in `IPawnMotor` over Unity's `CharacterController` |

### Possession

| Type | Purpose |
|---|---|
| `IPawnPossessor` | `OnPossessed` / `OnReleased` — implemented by players and AI alike |
| `PossessionResult` | `Success` / `AlreadyPossessed` / `NotSpawned` / `PawnDead` / `AlreadyOwner` |

### Lifecycle services

| Type | Purpose |
|---|---|
| `PawnRegistry` | Tracks live pawns; `Query`, `FindNearest` |
| `PawnSpawner` | Spawns/despawns, drives respawn scheduling |
| `ISpawnPointProvider` / `TransformSpawnPointProvider` | Where new pawns appear |
| `IRespawnPolicy` / `DelayedRespawnPolicy` | Whether/when a dead pawn returns |
| `IPawnPool` / `SimplePawnPool` | Reuse pawn instances instead of Instantiate/Destroy |

### Persistence

| Type | Purpose |
|---|---|
| `PawnSaveData` | Serializable snapshot of a pawn's state |
| `CaptureState()` / `RestoreState()` | Extension methods on `Pawn` |

### Optional adapters

| Assembly | Type | Purpose |
|---|---|---|
| `EldritchGames.PawnSystem.InputSystem` | `ControllerPossessor` | `IPawnPossessor` wrapping an Eldritch Input System `IController` |
| | `PawnInputTarget` | Forwards `ICommand`s into a pawn's capabilities |
| `EldritchGames.PawnSystem.AbilitySystem` | `AttributeVitalSource` | `IVitalSource` backed by an `AbilitySystemComponent` attribute |
| | `AbilityVitalsBinder` | Inspector-driven wiring for the above |

## Package layout

- `Runtime/` — the core (no dependencies beyond UnityEngine), plus the
  `InputSystem/` and `AbilitySystem/` adapter assemblies.
- `Editor/` — the `[SerializeReference]` picker, the `PawnDefinition` inspector,
  and the definition validator.
- `Tests/EditMode` and `Tests/PlayMode` — the automated test suite.
