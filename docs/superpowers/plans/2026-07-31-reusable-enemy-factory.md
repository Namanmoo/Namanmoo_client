# Reusable Enemy Factory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a stage-independent enemy factory with reusable contact-melee and approach-then-shoot behavior, definition-specific body/projectile sprites, and an animation extension point.

**Architecture:** `EnemyDefinition` stores reusable art and statistics while `EnemySpawnRequest` stores per-instance placement. `EnemyFactory` assembles shared components and selects a focused behavior controller; ranged attacks use a shared configurable projectile and all attacks notify `EnemyVisualController`.

**Tech Stack:** Unity 6, C#, ScriptableObject, Rigidbody2D/Collider2D, Animator, NUnit Unity Test Framework

## Global Constraints

- Do not redesign map generation, room placement, or spawn-point authoring.
- Existing Krab positions and encounter gate behavior must remain unchanged.
- An enemy using an existing behavior must be addable through a new definition without a new controller.
- Actual animation clips and authored attack motions are outside scope.
- Do not create a Git commit unless the user explicitly requests it.

---

### Task 1: Enemy Definition and Spawn Request

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyBehaviorType.cs`
- Create: `Assets/Scripts/Enemies/EnemyDefinition.cs`
- Create: `Assets/Scripts/Enemies/EnemySpawnRequest.cs`
- Test: `Assets/Tests/Editor/EnemyDefinitionTests.cs`

**Interfaces:**
- Produces: `EnemyBehaviorType.ChaseContact` and `EnemyBehaviorType.ApproachAndShoot`
- Produces: `EnemyDefinition.Configure(...)` for runtime/test/backend-style construction
- Produces: `EnemySpawnRequest(Transform parent, Transform target, Vector2 position, string instanceName = null)`

- [ ] **Step 1: Write the failing definition test**

Create two definitions that both use `ApproachAndShoot`, assign different body
and projectile sprites/statistics, and assert that every property remains
independent. Also assert that invalid negative numeric values are clamped by
`Configure`.

```csharp
[Test]
public void Configure_AllowsSharedBehaviorWithDifferentVisualsAndStats()
{
    EnemyDefinition first = ScriptableObject.CreateInstance<EnemyDefinition>();
    EnemyDefinition second = ScriptableObject.CreateInstance<EnemyDefinition>();
    first.Configure("squirrel", "Squirrel", bodyA, projectileA,
        EnemyBehaviorType.ApproachAndShoot, 10, 3f, 2, 6f, 1f, 8f, 4f, 0.2f);
    second.Configure("fox", "Fox", bodyB, projectileB,
        EnemyBehaviorType.ApproachAndShoot, 20, 2f, 5, 7f, 2f, 10f, 6f, 0.3f);

    Assert.That(first.BehaviorType, Is.EqualTo(second.BehaviorType));
    Assert.That(first.BodySprite, Is.Not.EqualTo(second.BodySprite));
    Assert.That(first.ProjectileSprite, Is.Not.EqualTo(second.ProjectileSprite));
    Assert.That(first.MaxHealth, Is.EqualTo(10));
    Assert.That(second.AttackDamage, Is.EqualTo(5));
}
```

- [ ] **Step 2: Run the test and verify RED**

Run Unity EditMode with filter `EnemyDefinitionTests`. Expected: compilation
fails because the three production types do not exist.

- [ ] **Step 3: Implement the data types**

Add serialized fields, read-only public properties, `[CreateAssetMenu]`, and a
`Configure` method. Clamp health to at least 1, speed/damage to at least 0,
attack interval to at least 0.01, range/lifetime/radius to at least 0.01.
`OnValidate` must apply the same normalization.

- [ ] **Step 4: Run `EnemyDefinitionTests` and verify GREEN**

Expected: all definition tests pass with no unexpected logs.

### Task 2: Shared Enemy Visual and Projectile

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyVisualController.cs`
- Create: `Assets/Scripts/Enemies/EnemyProjectile.cs`
- Test: `Assets/Tests/Editor/EnemyVisualControllerTests.cs`
- Test: `Assets/Tests/Editor/EnemyProjectileTests.cs`

**Interfaces:**
- Consumes: normalized projectile fields from `EnemyDefinition`
- Produces: `EnemyVisualController.Configure(Sprite, RuntimeAnimatorController)`
- Produces: `EnemyVisualController.PlayAttack()`
- Produces: `EnemyProjectile.Initialize(GameObject owner, Vector2 direction, int damage, float speed, float lifetime)`
- Produces: `EnemyProjectile.TryDamagePlayer(Collider2D other, float currentTime)`

- [ ] **Step 1: Write failing visual tests**

Assert `Configure` assigns the definition body sprite to the child
`SpriteRenderer`. Assert `PlayAttack()` is safe with no Animator Controller.
With a test Animator Controller, assert the `Attack` trigger is set through the
Animator path without coupling behavior controllers to Animator.

- [ ] **Step 2: Run `EnemyVisualControllerTests` and verify RED**

Expected: compilation fails because `EnemyVisualController` does not exist.

- [ ] **Step 3: Implement `EnemyVisualController`**

Cache/create `SpriteRenderer`; add/configure `Animator` only when a controller
is supplied. `PlayAttack` calls `animator.SetTrigger("Attack")` only when the
Animator exists and has an `Attack` trigger parameter.

- [ ] **Step 4: Run visual tests and verify GREEN**

Expected: static and Animator-backed visual cases pass.

- [ ] **Step 5: Write failing projectile tests**

Create two projectiles with different sprites, damage, speed, lifetime, and
collider radius. Assert their renderers/colliders preserve those values and
that `TryDamagePlayer` damages `PlayerHealth` but ignores scenery and owner
colliders.

- [ ] **Step 6: Run `EnemyProjectileTests` and verify RED**

Expected: compilation fails because `EnemyProjectile` does not exist.

- [ ] **Step 7: Implement `EnemyProjectile`**

Use a kinematic Rigidbody2D and trigger CircleCollider2D. Move along normalized
direction in `Update`, destroy after configured lifetime, ignore owner
colliders, and call `PlayerHealth.TryTakeDamage(damage, currentTime, 1f)`.

- [ ] **Step 8: Run projectile tests and verify GREEN**

Expected: all projectile configuration and collision tests pass.

### Task 3: Reusable Behavior Controllers

**Files:**
- Create: `Assets/Scripts/Enemies/ChaseContactEnemyController.cs`
- Create: `Assets/Scripts/Enemies/ApproachAndShootEnemyController.cs`
- Test: `Assets/Tests/Editor/ChaseContactEnemyControllerTests.cs`
- Test: `Assets/Tests/Editor/ApproachAndShootEnemyControllerTests.cs`

**Interfaces:**
- Consumes: `EnemyDefinition`, target Transform, Rigidbody2D, and `EnemyVisualController`
- Produces: `Initialize(EnemyDefinition definition, Transform target)` on both controllers
- Produces: pure calculation methods for deterministic movement/range tests
- Produces: `ApproachAndShootEnemyController.TryAttack(float currentTime)` for interval tests

- [ ] **Step 1: Write failing contact behavior tests**

Assert velocity points toward the target at definition movement speed. Assert
contact damage uses definition attack damage and the shared player
invulnerability interval.

- [ ] **Step 2: Run contact controller tests and verify RED**

Expected: compilation fails because `ChaseContactEnemyController` does not
exist.

- [ ] **Step 3: Implement contact behavior**

Move toward the target in `FixedUpdate`, reuse the existing collision-ignore
setup for the solid body, and deal definition-driven damage through a child
trigger sensor.

- [ ] **Step 4: Run contact controller tests and verify GREEN**

Expected: movement and contact damage cases pass.

- [ ] **Step 5: Write failing ranged behavior tests**

Assert:

```csharp
Assert.That(controller.CalculateVelocity(outsideRange), Is.Not.EqualTo(Vector2.zero));
Assert.That(controller.CalculateVelocity(insideRange), Is.EqualTo(Vector2.zero));
Assert.That(controller.TryAttack(0f), Is.True);
Assert.That(controller.TryAttack(0.5f), Is.False);
Assert.That(controller.TryAttack(definition.AttackInterval), Is.True);
```

Also assert the spawned projectile uses the definition's projectile sprite,
speed, damage, lifetime, and radius, and that an accepted attack calls the
visual controller.

- [ ] **Step 6: Run ranged controller tests and verify RED**

Expected: compilation fails because `ApproachAndShootEnemyController` does not
exist.

- [ ] **Step 7: Implement ranged behavior**

Approach while distance is greater than attack range. Stop inside range.
`TryAttack` fires toward the current target only when inside range and the
interval has elapsed, calls `PlayAttack()`, creates `EnemyProjectile`, and
advances `nextAttackTime`.

- [ ] **Step 8: Run ranged controller tests and verify GREEN**

Expected: approach, stop, resume, interval, projectile, and visual notification
cases all pass.

### Task 4: Enemy Factory and Existing Krab Compatibility

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyFactory.cs`
- Modify: `Assets/Scripts/Enemies/KrabFactory.cs`
- Modify: `Assets/Scripts/Stage1KrabEncounterSetup.cs`
- Test: `Assets/Tests/Editor/EnemyFactoryTests.cs`
- Modify: `Assets/Tests/Editor/Stage1KrabEncounterIntegrationTests.cs`

**Interfaces:**
- Consumes: `EnemyDefinition` and `EnemySpawnRequest`
- Produces: `EnemyFactory.Create(EnemyDefinition definition, EnemySpawnRequest request) : EnemyHealth`
- Preserves: `KrabFactory.Create(...) : EnemyHealth`

- [ ] **Step 1: Write failing factory tests**

For `ChaseContact`, assert the root has Rigidbody2D, CircleCollider2D,
EnemyHealth with definition max health, visual controller with body sprite,
and `ChaseContactEnemyController`. For `ApproachAndShoot`, assert the same
shared components plus `ApproachAndShootEnemyController` and no contact
controller. Assert null definition, null target, missing body sprite, and
invalid ranged projectile data throw clear argument exceptions.

- [ ] **Step 2: Run `EnemyFactoryTests` and verify RED**

Expected: compilation fails because `EnemyFactory` does not exist.

- [ ] **Step 3: Implement `EnemyFactory`**

Create the shared root and visual child, configure physics/health/visuals, then
switch only on `EnemyBehaviorType` to attach and initialize one behavior
controller. Return `EnemyHealth`.

- [ ] **Step 4: Run `EnemyFactoryTests` and verify GREEN**

Expected: both behavior routes and validation cases pass.

- [ ] **Step 5: Write failing Krab integration assertion**

Keep the five existing positions and names. Add assertions that each generated
Krab is created through the shared structure, has max health 5, retains the
Krab sprite, and uses `ChaseContactEnemyController`.

- [ ] **Step 6: Run `Stage1KrabEncounterIntegrationTests` and verify RED**

Expected: failure because existing `KrabFactory` still adds `KrabEnemy`.

- [ ] **Step 7: Adapt `KrabFactory` as a compatibility wrapper**

Create a runtime `EnemyDefinition` using the existing constants/sprite and
delegate construction to `EnemyFactory`. Do not change
`Stage1KrabEncounterSetup.SpawnPositions`, names, or gate registration.

- [ ] **Step 8: Run Krab and encounter tests and verify GREEN**

Run filters `KrabEnemyTests`, `Stage1KrabEncounterIntegrationTests`, and
`Stage1EncounterGateTests`. Expected: all existing behavior and gate tests
pass, updating legacy assertions only where they explicitly reference the
replaced controller type.

### Task 5: Final Verification and Usage Documentation

**Files:**
- Create: `docs/enemy-factory-usage.md`

**Interfaces:**
- Documents: creating `EnemyDefinition` assets and calling
  `EnemyFactory.Create`

- [ ] **Step 1: Write usage documentation**

Include Inspector asset creation, required fields, one contact enemy example,
one ranged enemy example with a unique projectile sprite, the spawn request
call, and the optional Animator Controller `Attack` trigger contract.

- [ ] **Step 2: Run focused Unity suites**

Run:

```text
EnemyDefinitionTests
EnemyVisualControllerTests
EnemyProjectileTests
ChaseContactEnemyControllerTests
ApproachAndShootEnemyControllerTests
EnemyFactoryTests
Stage1KrabEncounterIntegrationTests
Stage1EncounterGateTests
```

Expected: every suite reports zero failures.

- [ ] **Step 3: Run the full EditMode test suite**

Run Unity EditMode without a filter. Record any pre-existing unrelated failures
separately; no new enemy-related test may fail.

- [ ] **Step 4: Inspect the final diff**

Run `git diff --check` and `git status --short`. Remove only generated test
artifacts and Unity-generated project-file changes. Do not commit.

