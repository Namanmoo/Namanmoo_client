# Spinning Sword Projectile Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add arrow-key automatic sword firing with diagonal aim, spinning flight, configurable damage/fire rate, and enemy health.

**Architecture:** `PlayerSwordShooter` owns input, cooldown, and projectile creation. `SwordProjectile` owns deterministic motion, rotation, lifetime, and one-hit delivery, while `EnemyHealth` exclusively owns health and death.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity Input System, SpriteRenderer, Physics2D trigger colliders, NUnit EditMode and PlayMode tests.

## Global Constraints

- Preserve the root `sword.png`.
- Use `Assets/Weapons/sword.png` as a transparent Single Sprite.
- Default sword damage is exactly `5`.
- Damage and shots per second are serialized and editable on the player.
- Holding arrow keys automatically fires; combined axes produce normalized diagonal aim.
- WASD movement, player physics, player artwork, map scale, and item hotbar behavior remain unchanged.
- A projectile damages an `EnemyHealth` target at most once and does not physically push anything.

---

### Task 1: Add Enemy Health

**Files:**
- Create: `Assets/Scripts/Combat/EnemyHealth.cs`
- Create: `Assets/Tests/Editor/EnemyHealthTests.cs`

**Interfaces:**
- Produces: `EnemyHealth.CurrentHealth`, `EnemyHealth.MaxHealth`, and `EnemyHealth.TakeDamage(int amount)`.
- Consumes: Unity GameObject lifetime.

- [ ] **Step 1: Write failing health tests**

Cover these literal behaviors with real components:

```csharp
Assert.That(health.MaxHealth, Is.EqualTo(20));
Assert.That(health.CurrentHealth, Is.EqualTo(20));
health.TakeDamage(5);
Assert.That(health.CurrentHealth, Is.EqualTo(15));
health.TakeDamage(0);
health.TakeDamage(-3);
Assert.That(health.CurrentHealth, Is.EqualTo(15));
```

Add a lethal-damage test that yields one frame and asserts the enemy GameObject
is destroyed.

- [ ] **Step 2: Run `EnemyHealthTests` and verify RED**

Expected: compile failure because `EnemyHealth` does not exist.

- [ ] **Step 3: Implement minimal health behavior**

Create a MonoBehaviour with `[SerializeField, Min(1)] private int maxHealth = 20`,
read-only properties, initialization in `Awake`, ignored non-positive damage,
clamped subtraction, and `Destroy(gameObject)` at zero.

- [ ] **Step 4: Run `EnemyHealthTests` and verify GREEN**

Expected: all health tests pass.

### Task 2: Add the Spinning Sword Projectile

**Files:**
- Create: `Assets/Scripts/Combat/SwordProjectile.cs`
- Create: `Assets/Tests/Editor/SwordProjectileTests.cs`

**Interfaces:**
- Consumes: `EnemyHealth.TakeDamage(int)`.
- Produces: `SwordProjectile.Initialize(Vector2 direction, int damage, float speed, float spinSpeed, float lifetime, GameObject owner)`, `Advance(float deltaTime)`, and `TryHit(Collider2D other)`.

- [ ] **Step 1: Write failing projectile tests**

Verify `Advance(0.5f)` with direction `(1, 0)`, speed `8`, and spin `720`
moves exactly `4` units and rotates exactly `360` degrees modulo a full turn.
Verify a normalized diagonal moves the same distance. Verify lifetime expiry
destroys the projectile after the configured duration.

Create real enemy and collider components, initialize damage `5`, and assert:

```csharp
Assert.That(projectile.TryHit(enemyCollider), Is.True);
Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));
Assert.That(projectile.TryHit(enemyCollider), Is.False);
Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(15));
```

Also assert owner colliders and colliders without `EnemyHealth` return false.

- [ ] **Step 2: Run `SwordProjectileTests` and verify RED**

Expected: compile failure because `SwordProjectile` does not exist.

- [ ] **Step 3: Implement projectile behavior**

Normalize the initialization direction, move by
`direction * speed * deltaTime`, rotate around Z by
`spinSpeed * deltaTime`, count down lifetime, and destroy on expiry. Use a
consumed flag before calling `TakeDamage`. `OnTriggerEnter2D` delegates to
`TryHit`. Require a trigger `Collider2D` and kinematic `Rigidbody2D` on created
projectiles.

- [ ] **Step 4: Run `SwordProjectileTests` and verify GREEN**

Expected: all projectile tests pass.

### Task 3: Add Arrow-Key Automatic Firing and Import the Sword

**Files:**
- Create: `Assets/Weapons/sword.png`
- Create: `Assets/Weapons/sword.png.meta`
- Create: `Assets/Scripts/Combat/PlayerSwordShooter.cs`
- Create: `Assets/Tests/Editor/PlayerSwordShooterTests.cs`

**Interfaces:**
- Consumes: `SwordProjectile.Initialize(...)` and Unity `Keyboard`.
- Produces: `PlayerSwordShooter.CalculateDirection(Keyboard keyboard)`,
  `ProcessInput(Keyboard keyboard, float currentTime)`, and read-only
  configuration properties.

- [ ] **Step 1: Copy and import the sword asset**

Copy the root file byte-for-byte to `Assets/Weapons/sword.png`. Configure it as
a Single Sprite with alpha transparency, mipmaps off, clamp wrapping, 100 pixels
per unit, max texture size at least 512, and uncompressed texture compression.

- [ ] **Step 2: Write failing shooter tests**

Using a real Input System keyboard, verify right arrow yields `Vector2.right`
and up+right yields `(0.7071068f, 0.7071068f)` within `0.0001f`.

Configure damage `5`, shots per second `2`, speed `8`, spin `720`, and lifetime
`4`. Hold right and call `ProcessInput` at times `0`, `0.49`, and `0.5`; assert
the projectile counts are `1`, `1`, and `2`. Assert spawned objects use the
imported Sprite, a trigger collider, kinematic Rigidbody2D, and initialized
projectile values.

- [ ] **Step 3: Run `PlayerSwordShooterTests` and verify RED**

Expected: compile failure because `PlayerSwordShooter` does not exist.

- [ ] **Step 4: Implement shooter behavior**

Add serialized fields with defaults: damage `5`, shots per second `3`, speed
`8`, spin speed `720`, lifetime `4`, spawn offset `0.8`, and sword Sprite.
`Update` calls `ProcessInput(Keyboard.current, Time.time)`. Fire immediately
when a direction becomes active, then at intervals of `1 / shotsPerSecond`.
Create a `Sword Projectile` GameObject with SpriteRenderer sorting order `5`,
trigger `CapsuleCollider2D`, kinematic Rigidbody2D, and `SwordProjectile`.

- [ ] **Step 5: Run `PlayerSwordShooterTests` and verify GREEN**

Expected: all shooter and asset tests pass.

### Task 4: Integrate with Stage1 and Verify Regressions

**Files:**
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- Modify: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes: Sprite at `Assets/Weapons/sword.png` and `PlayerSwordShooter`.
- Produces: saved and runtime-created players with the same sword configuration.

- [ ] **Step 1: Write failing scene integration tests**

Assert the saved player has `PlayerSwordShooter`, its sword Sprite is the asset
at `Assets/Weapons/sword.png`, default damage is `5`, and shots per second is
positive. Extend runtime editor validation to assign the sword Sprite and to
throw an actionable error mentioning the path when it is missing.

- [ ] **Step 2: Run `Stage1SceneBuilderTests` and verify RED**

Expected: missing shooter and runtime sword field/guard.

- [ ] **Step 3: Implement both integration paths**

Load and validate the sword Sprite in `Stage1SceneBuilder`, add
`PlayerSwordShooter` to the player, and assign the Sprite. Add serialized
`swordSprite` to `Stage1RuntimeBootstrap`, populate it in editor validation,
guard it in `OnEnable`, and assign it to the runtime-created shooter.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run combat tests and `Stage1SceneBuilderTests`. Expected: zero failures.

- [ ] **Step 5: Rebuild and inspect Stage1**

Run `Stage1SceneBuilder.Build`. Verify the saved player has the shooter,
damage `5`, a positive firing rate, and the exact sword Sprite GUID.

- [ ] **Step 6: Run full regression suites**

Run all EditMode tests and all PlayMode tests. Confirm zero failures and scan
logs for compiler errors, `NullReferenceException`, `MissingReferenceException`,
and assertion failures.

