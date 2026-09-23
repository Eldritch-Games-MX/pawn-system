# Architecture

## Core types

- **`Pawn`** — the one MonoBehaviour. Owns the lifecycle state, the possessor,
  the tag container, the team, the vital source and the damage pipeline. Sealed:
  games extend a pawn by adding components, not by subclassing.
- **`PawnDefinition`** (ScriptableObject) — the authored recipe: id, display
  name, icon, team, tags, max vital, damage modifiers, and the lifecycle
  switches. Read on spawn, never written to.
- **`IPawnPossessor`** — whatever drives the pawn. Two callbacks, no state.
- **`IVitalSource`** — the clamped number the pawn dies when it runs out of.
- **`IPawnMotor`** — how the pawn moves, if it moves at all.

## Possession is symmetric

`Pawn` has no `PlayerController` field and no AI field. It has one
`IPawnPossessor`, and player control and AI control are the same operation with
different implementations. That is what lets a body be handed between a brain and
a player at runtime, and it is why possession lives here rather than in the input
package.

The rules:

- One possessor at a time. `TryPossess` refuses an occupied pawn and says so;
  `Possess` releases the sitting possessor first.
- An unspawned, despawned or dead pawn cannot be possessed.
- Death releases the possessor unless `ReleaseOnDeath` is off. Despawn always
  releases. Destroying the GameObject releases.
- `CurrentPossessor` is set before `OnPossessed` runs, so a possessor can use the
  pawn from inside its own callback. If that callback throws, the possession is
  rolled back.

## Capabilities, not inheritance

`Pawn.TryGet<T>` checks the pawn itself, then components on the GameObject and
its children. A game adds a capability by adding a component:

```csharp
public sealed class Climber : MonoBehaviour, IClimbable { /* ... */ }
if (pawn.TryGet<IClimbable>(out var climber)) climber.Climb(ledge);
```

This is deliberately the same shape as `ICapabilityProvider.TryGet` in the
Eldritch Input System and `IInteractable.TryGet` in the Interaction System, so a
command written against one works against all three. Keep that shape when adding
lookups; it is the ecosystem's one polymorphism idiom.

## The host owns the clock

Nothing in the package has an `Update`. `Pawn.Tick(float amount)` and
`PawnSpawner.Tick(float amount)` take whatever the game's clock counts — seconds
in a real-time game, rounds in a turn-based one. The parameter is named `amount`,
not `deltaTime`, for exactly that reason. `IPawnMotor.Move` is the one place the
word `deltaTime` appears, because a motor genuinely integrates over time.

This is the same decision the Ability System made, and it is why one pawn can be
proven to work under both clocks in a single test suite.

## Damage flows one way

```
weapon → DamageReceiver (hit zone multiplier, body part tag)
       → Pawn.ApplyDamage
            alive? positive? not invulnerable?
            → definition modifiers, top to bottom
            → component modifiers, in component order
            → IVitalSource.ApplyDelta
            → DamageTaken
            → Die(DeathInfo) when vitals hit zero
```

Order is part of the contract, not an accident: a flat reduction before a
percentage is not the same as after. Modifiers must be pure with respect to the
pawn — return a modified copy, never apply damage or kill from inside `Modify`.

A hit absorbed to zero still raises `DamageTaken`, so "blocked!" feedback works.

The component modifier list is cached on spawn; call
`Pawn.RefreshComponentModifiers` after adding or removing one at runtime. This is
a deliberate fix to the older pawn prototype, which called `GetComponents` on
every hit.

## Vitals are a seam, not a class

`Health` is the default, but a pawn can be given any `IVitalSource`. The
`AttributeVitalSource` adapter binds vitals to an Ability System attribute so a
project using both packages has one health number instead of two that drift. The
pawn subscribes to `IVitalSource.Changed`, so a pool emptied by an ability cost
kills the pawn exactly as damage would.

Rules — invulnerability, death, revival — live on `Pawn`, never on the vital
source. An implementation only has to clamp a number and report changes.

## Healing never revives

In the earlier prototype, `Heal` on a dead character silently brought it back,
because `IsDead` was derived from `Current <= 0`. Here death is an explicit state,
`Heal` no-ops on a corpse, and `TryRevive` is the only way back. Keep that line:
if something in your game resurrects, it should say so.

## Registry and spawner own no policy

`PawnRegistry` is a plain object, not a singleton — one per world, arena or
level. `PawnSpawner` delegates every decision it makes: *where* to
`ISpawnPointProvider`, *whether and when* to `IRespawnPolicy`, *from where* to
`IPawnPool`. Each has exactly one minimal built-in implementation, which is the
extent of the opinion this package has about your game.

## Extensibility: `[SerializeReference]` polymorphic lists

Damage modifiers are authored through `[SerializeReference]` fields decorated
with `[SerializeReferenceDropdown]`, rendered by a custom drawer as a type picker
built from `TypeCache`. A downstream project adds a modifier by dropping a
`[Serializable]` class into its own assembly.

**If you rename or move such a class**, add
`[UnityEngine.Scripting.APIUpdating.MovedFrom]` to it — otherwise existing assets
silently lose that entry. Run **Eldritch Games > Pawn System > Validate Pawn
Definitions** after any such rename.

An optional `[PawnDoc("...")]` attribute surfaces a one-line summary in the
picker. It is an authoring aid and is never read by runtime code.

## Deliberate duplication

`PawnTag`, `SerializeReferenceDropdownAttribute` and `PawnDocAttribute` mirror
types in the Ability System instead of referencing them. That keeps
`"dependencies": {}` true, which is what lets this package ship into a project
that has no ability system at all. The duplication is three small types and is
worth it; resist growing the list.

## Adapter assemblies

`EldritchGames.PawnSystem.InputSystem` and
`EldritchGames.PawnSystem.AbilitySystem` each reference exactly one sibling
package and are guarded by `versionDefines` → `defineConstraints`. In a project
without that package the assembly is not compiled at all, so its missing
references never become errors. Follow the same pattern for any future adapter —
never add a dependency to the core.

## Non-goals

Input binding, abilities, interaction, inventory, animation, AI behaviour,
cameras and networking do not belong here. The pawn exposes capabilities so those
systems can reach it. In particular: this package will not grow a behaviour tree,
and it will not grow an input abstraction of its own — resist both.
