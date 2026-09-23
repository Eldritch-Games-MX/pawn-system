# Extending pawns

Everything here is done from your own assembly. None of it requires editing this
package — if you find yourself wanting to, that is a bug report.

## Add a capability

A capability is any interface your game defines, implemented by a component on
the pawn.

```csharp
public interface IClimbable { void Climb(Ledge ledge); }

public sealed class Climber : MonoBehaviour, IClimbable
{
    public void Climb(Ledge ledge) { /* ... */ }
}

// anywhere
if (pawn.TryGet<IClimbable>(out var climber)) climber.Climb(ledge);
```

`TryGet` checks the pawn, then components on its GameObject and children. A pawn
that lacks the capability answers `false`; callers no-op rather than branching on
type.

## Write an AI brain

```csharp
public sealed class PatrolBrain : IPawnPossessor
{
    private Pawn pawn;

    public void OnPossessed(Pawn pawn) => this.pawn = pawn;
    public void OnReleased(Pawn pawn) => this.pawn = null;

    public void Tick(float amount)
    {
        if (pawn == null || !pawn.IsAlive) return;
        if (pawn.TryGet<IPawnMotor>(out var motor))
            motor.Move(NextWaypointDirection() * 2f, amount);
    }
}

pawn.TryPossess(new PatrolBrain());
```

The brain owns its own ticking. The pawn does not tick its possessor, because who
drives the AI budget is the game's decision, not the package's.

## Write a damage modifier

```csharp
[Serializable]
[PawnDoc("Reduces damage by a percentage of the incoming amount.")]
public sealed class PercentageResistance : IDamageModifier
{
    [SerializeField, Range(0f, 1f)] private float resistance = 0.25f;
    [SerializeField] private DamageTypeDefinition damageType;

    public DamageInfo Modify(Pawn target, DamageInfo damage)
    {
        if (damageType != null && damage.Type != damageType) return damage;
        return damage.WithAmount(damage.Amount * (1f - resistance));
    }
}
```

It appears in the Damage Modifiers dropdown automatically. Put it on the
definition when every pawn of an archetype has it, or on the GameObject as a
component when it comes and goes — component modifiers always run after the
authored ones.

Modifiers must be pure with respect to the pawn: return a copy, never apply
damage or kill from inside `Modify`.

## Replace the team rules

```csharp
public sealed class ReputationResolver : ITeamResolver
{
    public PawnRelationship Resolve(Pawn subject, Pawn other)
    {
        if (subject == null || other == null) return PawnRelationship.Neutral;
        int standing = Reputation.Between(subject.Team, other.Team);
        if (standing < -50) return PawnRelationship.Hostile;
        if (standing > 50) return PawnRelationship.Friendly;
        return PawnRelationship.Neutral;
    }
}

pawn.TeamResolver = new ReputationResolver();
```

Collapse whatever model you have onto the three answers at the moment something
asks. Resolvers must tolerate nulls and teamless pawns.

## Replace the spawning rules

```csharp
public sealed class SafestPointProvider : ISpawnPointProvider
{
    public bool TryGetSpawnPoint(Pawn prefab, out Vector3 position, out Quaternion rotation)
    {
        // return false when every point is contested — the spawner then waits
    }
}

public sealed class WavePolicy : IRespawnPolicy
{
    public bool ShouldRespawn(Pawn pawn) => waveInProgress;
    public float GetDelay(Pawn pawn) => 1.5f;
}

spawner.SpawnPointProvider = new SafestPointProvider();
spawner.RespawnPolicy = new WavePolicy();
```

## Replace the vitals

```csharp
// shared squad health, a shield layer, a damage-only-in-cutscenes pool…
public sealed class SquadVitalSource : IVitalSource { /* ... */ }

pawn.SetVitalSource(new SquadVitalSource(squad));   // in Awake, before the pawn spawns
```

For an Ability System-backed pool, use the `AbilityVitalsBinder` component or
`AttributeVitalSource` directly instead of writing your own.

## Add a secondary resource

No code at all, most of the time — create a `ResourceDefinition` asset (**Assets
&gt; Create &gt; Eldritch Games &gt; Pawn System &gt; Resource Definition**), list
it in the pawn's `Initial Resources`, and spend it:

```csharp
pawn.Resources.ApplyDelta(mana, -cost);
if (pawn.Resources.GetCurrent(mana) >= cost) { /* can afford it */ }
```

Register one at runtime instead when it is not part of every pawn of an
archetype — a temporary shield charge, a quest-specific counter:

```csharp
pawn.Resources.Register(shieldCharge, max: 3f);
```

## Mark networked ownership

The package never checks authority itself — write a thin `IPawnAuthority` over
whatever netcode you use and check it yourself before mutating a pawn:

```csharp
public sealed class NetcodePawnAuthority : IPawnAuthority
{
    private readonly NetworkObject networkObject;
    public NetcodePawnAuthority(NetworkObject networkObject) => this.networkObject = networkObject;
    public bool HasAuthority => networkObject.IsServer;
}

pawn.Authority = new NetcodePawnAuthority(networkObject);

// in your own RPC handler or damage entry point:
if (pawn.HasAuthority) pawn.ApplyDamage(damage);
```

## Write a motor

```csharp
public sealed class RigidbodyMotor : MonoBehaviour, IPawnMotor
{
    public Vector3 Velocity => body.linearVelocity;
    public bool IsGrounded => grounded;

    public void Move(Vector3 worldVelocity, float deltaTime) { /* ... */ }
    public void SetLookRotation(Quaternion rotation) { /* ... */ }
    public void Teleport(Vector3 position, Quaternion rotation) { /* clear velocity */ }
    public void Stop() { /* ... */ }
}
```

Take the velocity you are given and the time you are given. Speed belongs to the
game's stat rules, and reading `Time.deltaTime` inside a motor makes it
untestable and wrong under a fixed step.

## Test what you wrote

`EldritchGames.PawnSystem.TestUtilities` ships the pieces the package's own tests
use: `PawnDefinitionBuilder`, `TestTeams`, `TestPawns`, `FakePawnPossessor`,
`FakePawnMotor`, `FakeDamageModifier` and `FakeRespawnPolicy`. Reference that
assembly from your test asmdef and most pawn behaviour can be tested in EditMode
with no scene at all.
