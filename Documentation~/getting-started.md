# Getting started

## 1 — Author a definition

**Assets > Create > Eldritch Games > Pawn System > Pawn Definition**.

- **Id** — a stable string. Save data resolves pawns by it, so it must not be
  empty and must not change once you ship a save format.
- **Max Vital** — health, hull integrity, sanity: whatever this pawn dies when it
  runs out of.
- **Team** — optional `TeamDefinition` asset. Without one, every relationship
  resolves to Neutral.
- **Tags** — dotted labels such as `Class.Rogue` or `Faction.Undead`, copied into
  the pawn when it spawns.
- **Damage Modifiers** — the authored damage pipeline. `FlatDamageReduction`
  ships with the package; your own implementations appear in the same dropdown.
- **Spawn Invulnerability**, **Release On Death**, **Deactivate On Despawn** —
  the lifecycle switches.

## 2 — Build the prefab

Add a `Pawn` component and assign the definition. Add capability components
beside it:

```csharp
// A 3D character that walks
CharacterController + CharacterControllerMotor + Pawn

// A hit zone on the head bone
DamageReceiver { bodyPart = "Body.Head", damageMultiplier = 2 }

// Driven by a player through the Eldritch Input System
PawnInputTarget
```

Leave **Spawn On Start** on for pawns you drop straight into a scene; turn it off
for pawns a `PawnSpawner` or pool owns.

## 3 — Bring it into play

```csharp
Pawn pawn = Instantiate(knightPrefab).GetComponent<Pawn>();
SpawnResult result = pawn.Spawn(spawnPoint.position, spawnPoint.rotation);
```

`Spawn` fills vitals, adopts the definition's team and tags, grants the spawn
grace period and raises `Spawned`. It returns `AlreadySpawned` for a pawn that is
already in play and `MissingDefinition` for one with nothing to spawn from —
neither changes anything.

## 4 — Give it a driver

```csharp
// A player, through the Eldritch Input System
var possessor = new ControllerPossessor(playerController);
pawn.TryPossess(possessor);

// An AI, through your own brain
pawn.TryPossess(new WanderingBrain());

// Swap bodies without releasing first
otherPawn.Possess(possessor);

// Hand the body back
pawn.Release();
```

`TryPossess` refuses a pawn that is already possessed, not spawned, or dead, and
says which. `Possess` swaps instead of refusing.

## 5 — Hurt it, kill it, bring it back

```csharp
float applied = pawn.ApplyDamage(new DamageInfo(25f, fireType, instigator: dragon));
pawn.Heal(10f);                       // living pawns only
pawn.Kill(new DeathInfo(instigator: scriptedTrap));   // ignores invulnerability
pawn.TryRevive(new ReviveInfo(0.5f)); // back at half vitals
```

Healing a corpse does nothing. Revival is `TryRevive` and nothing else.

## 6 — Drive the clock

```csharp
// real-time
void Update() => pawn.Tick(Time.deltaTime);

// turn-based
void EndRound() => pawn.Tick(1f);
```

`Tick` advances timed state — today, the invulnerability grace period. A
`PawnSpawner` needs the same call to advance pending respawns.

## 7 — Spawn and respawn a population

```csharp
spawner.Prefab = gruntPrefab;
spawner.SpawnPointProvider = new TransformSpawnPointProvider(arenaCorners);
spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 5f, maxRespawns: 3);
spawner.Pool = new SimplePawnPool(poolRoot);

Pawn grunt = spawner.Spawn();
void Update() => spawner.Tick(Time.deltaTime);
```

Query the population without allocating:

```csharp
var buffer = new List<Pawn>();
spawner.Registry.Query(buffer, team: monsters, state: PawnState.Alive);
Pawn nearest = spawner.Registry.FindNearest(player.transform.position, p => p.IsHostileTo(player));
```

## 8 — Save and load

```csharp
PawnSaveData data = pawn.CaptureState();
string json = JsonUtility.ToJson(data);
// ... later ...
pawn.RestoreState(JsonUtility.FromJson<PawnSaveData>(json), teamLookup);
```

Restoring drives the pawn through its normal lifecycle methods, so a pawn
restored as dead raises `Died` during the load. Restore before wiring up death
feedback, or suppress it while loading.
