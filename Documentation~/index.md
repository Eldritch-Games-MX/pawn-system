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
6. **Zero package dependencies in the core.** Input, abilities and interaction
   are optional adapter assemblies that drop out of the build when their
   packages are absent.

## What a pawn owns

| Concern | Type | Notes |
|---|---|---|
| Identity | `PawnDefinition`, `PawnTag`, `TeamDefinition` | Authored once, shared by every instance of an archetype |
| Lifecycle | `PawnState`, `Spawn` / `Despawn` / `Kill` / `TryRevive` | One state at a time, every transition through a method; `Incapacitated` is an opt-in extra step between `Alive` and `Dead` |
| Possession | `IPawnPossessor`, `TryPossess` / `Possess` / `Release` | One driver at a time, player or AI |
| Vitals | `IVitalSource`, `Health`, `DamageInfo`, `IDamageModifier` | A clamped number plus a damage pipeline |
| Resources | `ResourceDefinition`, `PawnResourcePool` | Any number of secondary pools that never kill the pawn |
| Movement | `IPawnMotor`, `CharacterControllerMotor` | Entirely optional |
| Services | `PawnRegistry`, `PawnSpawner`, `IRespawnPolicy`, `IPawnPool` | Queryable population, spawning and respawning |
| Persistence | `CaptureState` / `RestoreState`, `PawnSaveData` | Opt-in extension methods |

## Non-goals

Input binding, abilities and effects, inventory, AI behaviour, and cameras
remain out of scope entirely — each is another package or game code, and the
pawn exposes capabilities so they can reach it. Two things get the narrowest
possible seam rather than a flat "no": **animation** is one optional
MonoBehaviour (`AnimatorPawnBridge`) that wires lifecycle events onto an
`Animator` by parameter name — not an animation system — and **networking**
is a data-only marker (`IPawnAuthority`) a netcode adapter can read — not an
enforcement mechanism or a netcode integration. Neither pulls in a dependency
or makes a policy decision the game did not make itself.

## Where to go next

- `getting-started.md` — build a pawn, spawn it, possess it, kill it, revive it.
- `architecture.md` — how the pieces fit, and the guardrails worth keeping.
- `pawn-taxonomy.md` — common character shapes and how they map onto the system.
- `extending-pawns.md` — adding capabilities, modifiers, policies and resolvers
  from a downstream project.
