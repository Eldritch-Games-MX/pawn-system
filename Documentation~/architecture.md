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
- **`PawnResourcePool`** — any number of *other* clamped numbers that never kill it, keyed by `ResourceDefinition`.
- **`IPawnMotor`** — how the pawn moves, if it moves at all.
- **`IPawnAuthority`** — an optional networked-ownership marker; see [Networking is a marker, not a system](#networking-is-a-marker-not-a-system).

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
            already Incapacitated? → any qualifying hit finishes straight to Dead, pipeline skipped
            alive? positive? not invulnerable?
            → definition modifiers, top to bottom
            → component modifiers, in component order
            → IVitalSource.ApplyDelta
            → DamageTaken
            → fatal? CanBeIncapacitated? → Incapacitate(DeathInfo) : Die(DeathInfo)
```

Order is part of the contract, not an accident: a flat reduction before a
percentage is not the same as after. Modifiers must be pure with respect to the
pawn — return a modified copy, never apply damage or kill from inside `Modify`.

A hit absorbed to zero still raises `DamageTaken`, so "blocked!" feedback works.

The component modifier list is cached on spawn; call
`Pawn.RefreshComponentModifiers` after adding or removing one at runtime. This is
a deliberate fix to the older pawn prototype, which called `GetComponents` on
every hit.

## Incapacitation is an extra step, not a second pipeline

`PawnState.Incapacitated` sits between `Alive` and `Dead`, opt-in per
`PawnDefinition.CanBeIncapacitated`. It reuses everything above rather than
duplicating it:

- A fatal blow on an incapacitation-capable pawn calls `Incapacitate(DeathInfo)`
  instead of `Die(DeathInfo)` — same modifier pipeline, same `DeathInfo`, one
  branch at the very end.
- A pawn that is *already* incapacitated takes a shortcut in `ApplyDamage`:
  invulnerability is still checked, but the modifier pipeline is skipped
  entirely — armor does not save a downed target — and any qualifying hit calls
  `Die` directly.
- `TryRevive` accepts `Dead` or `Incapacitated` as its source state and produces
  the same `Revived` event either way. There is no separate "pick up a downed
  teammate" API — recovering a corpse and recovering a downed pawn are the same
  operation from the pawn's point of view.
- `Kill()` always goes straight to `Dead`, whether called on a living pawn or to
  finish one already down. It is the one path that never routes through
  `Incapacitated`, by design — it is the "no matter what" method.
- An optional bleed-out timer (`PawnDefinition.IncapacitationDuration`) is just
  another field `Tick` counts down, exactly like the invulnerability grace
  period already did.

Nothing that does not set `CanBeIncapacitated` ever sees any of this — a fatal
blow kills on the spot exactly as it always did.

## Secondary resources reuse `Health`

`PawnResourcePool` does not reimplement clamping; each registered resource is a
`Health` instance internally, keyed by its `ResourceDefinition`. The pool adds
nothing on top except the registration bookkeeping and a `Changed` event that
also reports *which* resource changed. This mirrors the Ability System's
`AttributeSet` in shape (`Register` / `IsRegistered` / `GetCurrentValue` /
`ModifyBaseValue`-equivalents) without referencing it, for the same zero-dependency reason as `PawnTag`.

`PawnDefinition.InitialResources` is a plain list of `ResourceDefinition`
assets — no per-pawn override struct — because a resource's `DefaultMax` already
lives on the definition, the same way `AttributeDefinition.DefaultValue` does in
the Ability System. `Pawn.Spawn()` calls `RegisterOrRefill` for each one, so a
pooled pawn's mana comes back full exactly like its vitals do.

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
