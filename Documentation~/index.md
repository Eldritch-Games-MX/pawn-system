# Pawn System

A pawn is anything in the world that a player or an AI can drive. This package
gives that idea one component, one lifecycle and a small set of seams, and
refuses to know anything else about your game.

## Design principles

1. **One pawn for players and NPCs.** Possession is symmetric: a player
   controller and an AI brain are both `IPawnPossessor`, and `Pawn` cannot tell
   them apart. A body can change hands mid-game without either side knowing what
   the other is.
2. **No hard-coded genre.** No weapons, no dialogue, no turn order, no stealth,
   no inventory. Health is a clamped number behind a seam; a damage *type* is an
   asset, not an enum; a *team* is an asset with authored relationships; a *tag*
   is a dotted string the package never interprets.
3. **Composition, not modification.** `Pawn` is sealed. A game adds capabilities
   by adding components and reaching them through `TryGet<T>` — the same shape as
   `ICapabilityProvider.TryGet` in the Eldritch Input System and
   `IInteractable.TryGet` in the Interaction System.
4. **The host owns the clock.** Nothing in the package has an `Update`. You call
   `Tick(float amount)` — once per frame, once per round, or never.
5. **Transactional state changes.** All validation happens before any mutation.
   A call that returns a failure code has changed nothing and raised no events.
6. **Zero package dependencies in the core.** Input and abilities are optional
   adapter assemblies that drop out of the build when their packages are absent.

## What a pawn owns

| Concern | Type | Notes |
|---|---|---|
| Identity | `PawnDefinition`, `PawnTag`, `TeamDefinition` | Authored once, shared by every instance of an archetype |
| Lifecycle | `PawnState`, `Spawn` / `Despawn` / `Kill` / `TryRevive` | One state at a time, every transition through a method |
| Possession | `IPawnPossessor`, `TryPossess` / `Possess` / `Release` | One driver at a time, player or AI |
| Vitals | `IVitalSource`, `Health`, `DamageInfo`, `IDamageModifier` | A clamped number plus a damage pipeline |
| Movement | `IPawnMotor`, `CharacterControllerMotor` | Entirely optional |
| Services | `PawnRegistry`, `PawnSpawner`, `IRespawnPolicy`, `IPawnPool` | Queryable population, spawning and respawning |
| Persistence | `CaptureState` / `RestoreState`, `PawnSaveData` | Opt-in extension methods |

## Non-goals

Input binding, abilities and effects, interaction, inventory, animation, AI
behaviour, cameras, and networking. Each of those is another package or game
code; the pawn exposes capabilities so they can reach it.

## Where to go next

- `getting-started.md` — build a pawn, spawn it, possess it, kill it, revive it.
- `architecture.md` — how the pieces fit, and the guardrails worth keeping.
- `pawn-taxonomy.md` — common character shapes and how they map onto the system.
- `extending-pawns.md` — adding capabilities, modifiers, policies and resolvers
  from a downstream project.
