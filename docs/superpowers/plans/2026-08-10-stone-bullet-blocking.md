# Stone Bullet Blocking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `Obstacle_Rock` ("Stone") destroy both the player's
ranged-weapon projectiles and monster bullets on contact, unconditionally
(even if the weapon has bounce charges remaining). `Obstacle_Lake` needs no
changes — it already lets every bullet pass through.

**Architecture:** Add an empty marker component, `BulletBlockingObstacle`
(`NaManMoo.Dungeon` namespace, alongside the existing `SpikeObstacle`),
attach it only to `Obstacle_Rock.prefab`, and have each projectile script
(`WeaponProjectile`, `EnemyProjectile`, `BossBullet`, `SlimeBossProjectile`)
destroy itself when it hits a collider carrying that marker.

**Tech Stack:** Unity (C#), NUnit EditMode tests (`Assets/Tests/Editor`),
hand-authored `.prefab` YAML (this project has no prefab-creation tooling;
GUIDs are generated with `openssl rand -hex 16` and hand-written into
`.meta` files, the same way the existing `SpikeObstacle`/`Obstacle_Spike`
work was done).

## Global Constraints

- Do not modify `Obstacle_Lake.prefab` at all — it already satisfies the
  requirement with zero code changes.
- Do not change how Stone or Lake block player/monster *movement* — both
  already use a correctly-configured non-trigger `BoxCollider2D` for that.
- Do not change `TryBounce`'s existing behavior against any collider that
  does **not** carry the `BulletBlockingObstacle` marker (e.g. the room's
  Safety Boundary wall) — out of scope, not requested.
- Per explicit user decision: a bullet hitting a `BulletBlockingObstacle`
  is destroyed **unconditionally** — no ricochet, regardless of remaining
  bounce charges.
- No balance values (damage, speed, lifetime, etc.) are touched anywhere in
  this plan.
- Spec: `docs/superpowers/specs/2026-08-10-stone-bullet-blocking-design.md`

---

### Task 1: BulletBlockingObstacle marker component + Obstacle_Rock prefab

**Files:**
- Create: `Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs`
- Create: `Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs.meta`
- Modify: `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab`
- Test: `Assets/Tests/Editor/RoomObstaclePrefabsTests.cs`

**Interfaces:**
- Produces: `NaManMoo.Dungeon.BulletBlockingObstacle` — an empty
  `MonoBehaviour` marker, no fields, no methods beyond what `MonoBehaviour`
  provides. Tasks 2 and 3 detect it via
  `other.GetComponentInParent<BulletBlockingObstacle>() != null`.

- [ ] **Step 1: Write the failing test**

Open `Assets/Tests/Editor/RoomObstaclePrefabsTests.cs` and add a second
test method (keep the existing `SpikePrefab_...` test as-is):

```csharp
public sealed class RoomObstaclePrefabsTests
{
    private const string SpikePath = "Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab";
    private const string RockPath = "Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab";

    [Test]
    public void SpikePrefab_HasTriggerColliderAndSpikeObstacleWithConfiguredDamage()
    {
        // ... existing test body, unchanged ...
    }

    [Test]
    public void RockPrefab_HasNonTriggerColliderAndBulletBlockingObstacle()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPath);
        Assert.That(prefab, Is.Not.Null);

        Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);

        Collider2D collider = prefab.GetComponent<Collider2D>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.False, "Stone은 이동을 물리적으로 막아야 한다");

        BulletBlockingObstacle marker = prefab.GetComponent<BulletBlockingObstacle>();
        Assert.That(marker, Is.Not.Null);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `-testFilter "RoomObstaclePrefabsTests.RockPrefab_HasNonTriggerColliderAndBulletBlockingObstacle"`
Expected: FAIL to compile — `NaManMoo.Dungeon.BulletBlockingObstacle` does
not exist yet, and `Obstacle_Rock.prefab` doesn't have the component.

- [ ] **Step 3: Create the marker component**

Create `Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs`:

```csharp
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 원거리 무기 투사체와 몬스터 bullet을 막는 장애물임을 표시하는 마커.
    /// 로직은 없다 — WeaponProjectile/EnemyProjectile 등이 이 컴포넌트가
    /// 붙어 있는 콜라이더를 맞았는지만 확인한다.
    /// </summary>
    public sealed class BulletBlockingObstacle : MonoBehaviour
    {
    }
}
```

Create `Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs.meta`:

```yaml
fileFormatVersion: 2
guid: 64289ca45463528b9f7b8dc5b00415ab
```

- [ ] **Step 4: Attach the marker to Obstacle_Rock.prefab**

Open `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab`. In the
`GameObject` document (`&100000`), add a fourth entry to `m_Component`:

```yaml
  m_Component:
  - component: {fileID: 400000}
  - component: {fileID: 212000}
  - component: {fileID: 61000}
  - component: {fileID: 11400000}
```

Append a new `MonoBehaviour` document at the end of the file:

```yaml
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 64289ca45463528b9f7b8dc5b00415ab, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
```

Do not change anything else in the file — the existing `Transform`,
`SpriteRenderer`, and `BoxCollider2D` (`m_IsTrigger: 0`) blocks stay
exactly as they are.

- [ ] **Step 5: Run the test to verify it passes**

Run: `-testFilter "RoomObstaclePrefabsTests.RockPrefab_HasNonTriggerColliderAndBulletBlockingObstacle"`
Expected: PASS. Also check the batch-mode log for import errors on
`Obstacle_Rock.prefab` — a malformed YAML edit shows up there even if the
test above still happens to pass.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs" "Assets/Scripts/Dungeon/Obstacle/BulletBlockingObstacle.cs.meta" "Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab" "Assets/Tests/Editor/RoomObstaclePrefabsTests.cs"
git commit -m "feat: Stone에 BulletBlockingObstacle 마커 추가"
```

---

### Task 2: WeaponProjectile stops unconditionally on Stone

**Files:**
- Modify: `Assets/Scripts/Combat/WeaponProjectile.cs:227-241` (the
  `TryHit` method's no-`EnemyHealth` branch)
- Test: `Assets/Tests/Editor/WeaponProjectilePierceTests.cs`

**Interfaces:**
- Consumes: `NaManMoo.Dungeon.BulletBlockingObstacle` (Task 1).

- [ ] **Step 1: Write the failing test**

Open `Assets/Tests/Editor/WeaponProjectilePierceTests.cs`. Add
`using NaManMoo.Dungeon;` to the top of the file (after the existing
`using` lines), then add a new test method inside the class (near
`BoomerangReturnsInsteadOfExpiringOnHit`):

```csharp
    [Test]
    public void BulletBlockingObstacleDestroysProjectileEvenWithBounceChargesRemaining()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn(new ProjectileTuning { BounceCount = 3 });
        var obstacleObject = new GameObject("obstacle");
        Collider2D obstacleCollider = obstacleObject.AddComponent<BoxCollider2D>();
        obstacleObject.AddComponent<BulletBlockingObstacle>();

        Assert.That(projectile.TryHit(obstacleCollider), Is.True);
        // 이미 소멸했다 — 다시 맞아도 아무 일도 없다
        Assert.That(projectile.TryHit(obstacleCollider), Is.False);

        Object.DestroyImmediate(obstacleObject);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `-testFilter "WeaponProjectilePierceTests.BulletBlockingObstacleDestroysProjectileEvenWithBounceChargesRemaining"`
Expected: FAIL — `TryHit` currently calls `TryBounce` for this collider
(since it's a non-trigger, non-`EnemyHealth` collider with bounce charges
available) and returns `false` instead of `true`.

- [ ] **Step 3: Update TryHit**

In `Assets/Scripts/Combat/WeaponProjectile.cs`, add
`using NaManMoo.Dungeon;` to the top of the file (after
`using UnityEngine;`). Then change the no-`EnemyHealth` branch inside
`TryHit` (currently lines 235-239):

```csharp
        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
        {
            TryBounce(other);
            return false;
        }
```

to:

```csharp
        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
        {
            if (other.GetComponentInParent<BulletBlockingObstacle>() != null)
            {
                Expire();
                return true;
            }

            TryBounce(other);
            return false;
        }
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `-testFilter "WeaponProjectilePierceTests.BulletBlockingObstacleDestroysProjectileEvenWithBounceChargesRemaining"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Combat/WeaponProjectile.cs" "Assets/Tests/Editor/WeaponProjectilePierceTests.cs"
git commit -m "feat: 플레이어 무기 투사체가 Stone에 맞으면 도탄과 무관하게 소멸"
```

---

### Task 3: Monster bullets stop unconditionally on Stone

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyProjectile.cs:93-96` (`OnTriggerEnter2D`)
- Modify: `Assets/Scripts/Enemies/BossBullet.cs:32-38` (`OnTriggerEnter2D`)
- Modify: `Assets/Scripts/Enemies/SlimeBossProjectile.cs:73-80` (`OnTriggerEnter2D`)

**Interfaces:**
- Consumes: `NaManMoo.Dungeon.BulletBlockingObstacle` (Task 1).

This task's `OnTriggerEnter2D` changes aren't independently unit-testable
without simulating real Unity trigger physics events (the same limitation
already documented for `SpikeObstacle`'s trigger wiring in the prior
obstacles plan) — verify by code review against Task 2's already-tested
`WeaponProjectile.TryHit` pattern, and by the compile check in Step 2.

- [ ] **Step 1: Update all three OnTriggerEnter2D methods**

In `Assets/Scripts/Enemies/EnemyProjectile.cs`, add
`using NaManMoo.Dungeon;` to the top of the file, then change
`OnTriggerEnter2D` (currently lines 93-96) from:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other, Time.time);
    }
```

to:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryDamagePlayer(other, Time.time))
        {
            return;
        }

        if (other != null && other.GetComponentInParent<BulletBlockingObstacle>() != null)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }
```

In `Assets/Scripts/Enemies/BossBullet.cs`, add `using NaManMoo.Dungeon;`
to the top of the file, then change `OnTriggerEnter2D` (currently lines
32-38) from:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryDamagePlayer(other, Time.time))
        {
            gameObject.SetActive(false);
        }
    }
```

to:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryDamagePlayer(other, Time.time))
        {
            gameObject.SetActive(false);
            return;
        }

        if (other != null && other.GetComponentInParent<BulletBlockingObstacle>() != null)
        {
            gameObject.SetActive(false);
        }
    }
```

In `Assets/Scripts/Enemies/SlimeBossProjectile.cs`, add
`using NaManMoo.Dungeon;` to the top of the file, then change
`OnTriggerEnter2D` (currently lines 73-80) from:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other == null ? null : other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.TryTakeDamage(damage, Time.time, PlayerInvulnerabilityDuration))
        {
            Destroy(gameObject);
        }
    }
```

to:

```csharp
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other == null ? null : other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.TryTakeDamage(damage, Time.time, PlayerInvulnerabilityDuration))
        {
            Destroy(gameObject);
            return;
        }

        if (other != null && other.GetComponentInParent<BulletBlockingObstacle>() != null)
        {
            Destroy(gameObject);
        }
    }
```

- [ ] **Step 2: Verify compilation**

Run a batch-mode compile check (or run
`-testFilter "RoomObstaclePrefabsTests"`, which touches all the compiled
assemblies) and confirm there are no compile errors introduced by the
three edits above.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Enemies/EnemyProjectile.cs" "Assets/Scripts/Enemies/BossBullet.cs" "Assets/Scripts/Enemies/SlimeBossProjectile.cs"
git commit -m "feat: 몬스터 bullet이 Stone에 맞으면 소멸"
```
