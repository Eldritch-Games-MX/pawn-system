# Pawn taxonomy

Every row below is a character shape from a different genre, expressed with the
same `Pawn` component and no package changes. If a shape you need is not here and
does not fall out of these parts, that is worth a design conversation before
adding anything to the core.

| Shape | Definition | Components | Possession | Notes |
|---|---|---|---|---|
| **Player character (real-time 3D)** | Max Vital, team `Players`, short Spawn Invulnerability | `CharacterControllerMotor`, `PawnInputTarget`, hit-zone `DamageReceiver`s | `ControllerPossessor` over an input `IController` | The canonical case |
| **Player character (turn-based)** | Same, no invulnerability | No motor at all | Same | `Tick(1f)` per round; the battle system moves the model |
| **Melee NPC** | Team `Monsters`, tags `Beast` | Motor, your AI brain component | Your `IPawnPossessor` | Identical API to the player case |
| **Boss** | Large Max Vital, `FlatDamageReduction`, tag `Boss` | Hit zones with multipliers, phase components | AI possessor swapped per phase | Phases = swapping the possessor, not subclassing |
| **Companion** | Team `Players`, tag `Ally` | Motor, follow component | AI possessor the player can take over | The body never changes; only the driver does |
| **Turret / camera** | Max Vital, Release On Death off | No motor; an aim component | AI or player | A pawn with no movement is still a pawn |
| **Destructible prop** | Max Vital, no team | `DamageReceiver` only | None | Or skip `Pawn` and implement `IDamageable` directly |
| **Vehicle** | Own vitals and team | Vehicle motor implementing `IPawnMotor` | The driver's possessor, moved from the character pawn | Entering a vehicle = release one pawn, possess another |
| **Possessing ghost** | Ghost pawn with its own vitals | — | One possessor that moves between bodies | Exactly what symmetric possession is for |
| **Board piece** | Max Vital 1, team per side | None | Player or AI | `Spawn` / `Kill` / `TryRevive` model captures and promotions |
| **Wave enemy** | Small Max Vital | Motor | AI | `PawnSpawner` + `SimplePawnPool` + `DelayedRespawnPolicy` |
| **Permadeath character** | Release On Death on | — | Either | `IRespawnPolicy` that always refuses |
| **Lives-based arcade player** | — | — | Player | `DelayedRespawnPolicy(delay, maxRespawns: 3)` |
| **Mind-controlled enemy** | — | — | Player possessor swapped in | Also assign `pawn.Team` to flip its allegiance |
| **Invulnerable escort NPC** | — | — | AI | `pawn.SetInvulnerable(true)`; scripted deaths still land via a damage type that ignores invulnerability |
| **Ability-driven RPG character** | Max Vital ignored | `AbilitySystemComponent`, `AbilityVitalsBinder` | Either | Vitals are an attribute, so costs, effects and damage move one number |
| **Downable shooter player** | `CanBeIncapacitated`, `IncapacitationDuration` for a bleed-out timer | `CharacterControllerMotor`, `PawnInputTarget` | Same possessor throughout | A fatal blow downs, not kills; a teammate's `TryRevive` picks them back up |
| **Spellcaster with mana** | `InitialResources = [ mana ]` | Whatever else the archetype needs | Either | `pawn.Resources.ApplyDelta(mana, -cost)` before casting |
| **Networked co-op character** | Same as any player character | `PawnInputTarget` | `ControllerPossessor` | `pawn.Authority = new NetcodePawnAuthority(...)`; the game checks `HasAuthority` before calling `ApplyDamage`/`TryPossess` locally |
| **Animated 3D character** | Any of the above | `AnimatorPawnBridge` alongside `Animator` | Any | Name the parameters that exist on the controller; leave the rest blank |
| **Lootable NPC corpse** | Any | `PawnInteractable`, a game-defined `ICollectible` that reads `pawn.State` | Any while alive | Dying does not change which components are on the GameObject — the capability itself decides when it applies |

## Things that are tags, not features

`Status.Stunned`, `Class.Rogue`, `Faction.Undead`, `Boss`, `Invisible`,
`Objective` — all of these are `PawnTag`s a game adds and interprets. The package
never reads a tag's meaning, which is why it can stay genre-agnostic while the
game gets specific.

## Things that are capabilities, not pawn fields

Weapons, inventory, dialogue, stealth, climbing, swimming, aiming, animation
state, crafting. Each is a component implementing an interface the game defines,
reached with `pawn.TryGet<T>`. If you are tempted to add a field to `Pawn` for
one of these, add a component instead.
