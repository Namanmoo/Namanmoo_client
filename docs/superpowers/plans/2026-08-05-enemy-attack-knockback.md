# 적 피격 넉백 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어의 무기 공격이 적을 맞히면 공격 방향의 정반대로 적이 살짝(거리 0.3, 0.12초) 밀려나게 한다.

**Architecture:** 기존 `EnemyStatus`(경직·냉기·화상·독을 관리하는 컴포넌트)에 넉백 상태를 추가해 재사용한다. 새 정적 헬퍼 `EnemyKnockback.Apply(target, attackDirection)`가 "추적형 적인지 + 살아있는지 + 방향이 있는지"를 판정한 뒤 `EnemyStatus.ApplyKnockback`을 호출한다. 근접 판정(`MeleeStrike`), 도끼(`AxeSwing`), 검 발사체(`SwordProjectile`), 일반 무기 발사체(`WeaponProjectile`) 네 곳의 `TakeDamage` 호출 직후에 이 헬퍼를 한 줄씩 추가한다.

**Tech Stack:** Unity 6000.5.5f1, C# (전역 네임스페이스, MonoBehaviour), NUnit + Unity Test Framework(EditMode/PlayMode).

## Global Constraints

- 플레이어·몬스터·보스·무기의 기존 밸런스 수치(체력, 공격력, 이동속도, 사거리, 공격 간격 등)는 절대 변경하지 않는다.
- 이번 작업에서 수정한 파일과 직접 관련된 테스트만 실행한다. 전체 테스트 스위트나 관련 없는 회귀 테스트는 실행하지 않는다.
- Unity 테스트는 항상 `-testFilter`로 좁혀서 실행한다(`-runTests` 단독 실행 금지).
- 대상 공격은 직접 타격 4곳(근접/도끼/검 발사체/일반 발사체)뿐이며, 화상·독 지속피해와 폭발·연쇄 2차 효과, 기존 `ShockwaveAction`은 건드리지 않는다.
- 넉백 대상은 `ChaseContactEnemyController` · `ApproachAndShootEnemyController` · `KrabEnemy`가 붙은 "추적형" 적뿐이며, 보스와 고정형 적(`StationaryFourWayShooterController`)은 제외한다.
- 새 상수: 넉백 거리 0.3 유닛, 넉백 지속시간 0.12초. `EnemyKnockback` 클래스 안의 `private const`로만 관리한다.
- Unity 테스트 실행 커맨드 형식(이 저장소의 기존 관례):
  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter '<ClassName>' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.log'
  ```
  PlayMode 테스트는 `-testPlatform PlayMode`로 바꾼다.

---

## Task 1: EnemyStatus에 넉백 상태 추가

**Files:**
- Modify: `Assets/Scripts/Combat/Effects/EnemyStatus.cs:32` (필드), `:34-51` (SpeedMultiplier), `:56` (프로퍼티), `:132-141` (Apply 계열 메서드 옆), `:149-182` (Tick)
- Test: `Assets/Tests/Editor/EnemyStatusTests.cs`

**Interfaces:**
- Consumes: 없음(기존 `EnemyStatus` 컴포넌트만 확장).
- Produces:
  - `EnemyStatus.ApplyKnockback(Vector2 direction, float distance, float duration)` — 다음 태스크들이 넉백을 걸 때 쓰는 API.
  - `EnemyStatus.IsKnockedBack` (bool) — 넉백이 진행 중인지.
  - 기존 `EnemyStatus.SpeedMultiplier`가 넉백 중에는 0을 반환(경직과 동일한 규칙).

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/EnemyStatusTests.cs`의 `StaggerStopsMovementCompletely` 테스트 아래에 다음 다섯 테스트를 추가한다.

```csharp
    [Test]
    public void KnockbackSlidesTheDistanceOverItsDuration()
    {
        status.ApplyKnockback(Vector2.left, distance: 0.3f, duration: 0.12f);

        status.Tick(0.12f);

        Assert.That(enemy.transform.position.x, Is.EqualTo(-0.3f).Within(0.0001f));
        Assert.That(status.IsKnockedBack, Is.False);
    }

    [Test]
    public void KnockbackStopsMovementWhileActive()
    {
        status.ApplyKnockback(Vector2.left, distance: 0.3f, duration: 0.12f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(0f));

        status.Tick(0.12f);

        Assert.That(status.SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void ZeroDirectionKnockbackIsIgnored()
    {
        status.ApplyKnockback(Vector2.zero, distance: 0.3f, duration: 0.12f);

        Assert.That(status.IsKnockedBack, Is.False);
    }

    [Test]
    public void ReapplyingKnockbackOverwritesThePreviousSlide()
    {
        status.ApplyKnockback(Vector2.left, distance: 0.3f, duration: 0.12f);
        status.ApplyKnockback(Vector2.up, distance: 0.3f, duration: 0.12f);

        status.Tick(0.12f);

        // 나중에 건 위쪽 넉백만 반영된다 — 먼저 걸린 왼쪽 방향은 누적되지 않는다
        Assert.That(enemy.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(enemy.transform.position.y, Is.EqualTo(0.3f).Within(0.0001f));
    }
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'EnemyStatusTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemystatus-knockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemystatus-knockback-red.log'
```
Expected: `ApplyKnockback`/`IsKnockedBack`이 없어서 컴파일 에러로 FAIL.

### Step 3: 최소 구현

`Assets/Scripts/Combat/Effects/EnemyStatus.cs:32` 바로 아래(`private float staggerRemaining;` 다음 줄)에 필드 추가:

```csharp
    private float staggerRemaining;

    private Vector2 knockbackVelocity;
    private float knockbackRemaining;
```

`SpeedMultiplier` 게터(현재 34-51행)에서 경직 체크 부분을 다음처럼 바꾼다:

```csharp
    public float SpeedMultiplier
    {
        get
        {
            if (staggerRemaining > 0f || knockbackRemaining > 0f)
            {
                return 0f;
            }

            if (chillRemaining <= 0f)
            {
                return 1f;
            }

            return Mathf.Max(MinSpeedMultiplier, 1f - (chillSlowPercent / 100f));
        }
    }
```

`public bool IsStaggered => staggerRemaining > 0f;` 다음 줄(56행 부근)에 프로퍼티 추가:

```csharp
    public bool IsStaggered => staggerRemaining > 0f;
    public bool IsKnockedBack => knockbackRemaining > 0f;
```

`ApplyStagger` 메서드(132-141행) 바로 다음에 새 메서드 추가:

```csharp
    /// <summary>넉백 — 공격 반대 방향으로 짧게 밀어낸다. 다시 걸리면 최신 값으로 덮어쓴다.</summary>
    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        if (direction == Vector2.zero || distance <= 0f || duration <= 0f)
        {
            return;
        }

        knockbackVelocity = direction.normalized * (distance / duration);
        knockbackRemaining = duration;
    }
```

`Tick(float deltaTime)`(149-182행)에서 `staggerRemaining`/`chillRemaining` 감소 다음 줄에 슬라이드 로직을 추가한다. 메서드 전체는 다음과 같아진다(굵게 표시한 부분이 새로 추가하는 6줄이고, 그 아래 화상·독·`FlushDamage()` 블록은 기존 코드 그대로다):

```csharp
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        staggerRemaining = Mathf.Max(0f, staggerRemaining - deltaTime);
        chillRemaining = Mathf.Max(0f, chillRemaining - deltaTime);

        if (knockbackRemaining > 0f)
        {
            float knockbackSlice = Mathf.Min(deltaTime, knockbackRemaining);
            transform.position += (Vector3)(knockbackVelocity * knockbackSlice);
            knockbackRemaining -= knockbackSlice;
        }

        if (burnRemaining > 0f)
        {
            float slice = Mathf.Min(deltaTime, burnRemaining);
            pendingDamage += burnDamagePerSecond * slice;
            burnRemaining -= slice;
            if (burnRemaining <= 0f)
            {
                burnDamagePerSecond = 0f;
            }
        }

        if (poisonRemaining > 0f && poisonStacks > 0)
        {
            float slice = Mathf.Min(deltaTime, poisonRemaining);
            pendingDamage += poisonDamagePerStackPerSecond * poisonStacks * slice;
            poisonRemaining -= slice;
            if (poisonRemaining <= 0f)
            {
                poisonStacks = 0;
            }
        }

        FlushDamage();
    }
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'EnemyStatusTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemystatus-knockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemystatus-knockback-green.log'
```
Expected: 기존 테스트 포함 전부 PASS.

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/Effects/EnemyStatus.cs Assets/Tests/Editor/EnemyStatusTests.cs
git commit -m "feat: EnemyStatus에 넉백 상태 추가"
```

---

## Task 2: EnemyKnockback 헬퍼 — 대상 판정

**Files:**
- Create: `Assets/Scripts/Combat/EnemyKnockback.cs`
- Test: `Assets/Tests/Editor/EnemyKnockbackTests.cs`

**Interfaces:**
- Consumes: `EnemyStatus.EnsureOn(EnemyHealth)`, `EnemyStatus.ApplyKnockback(Vector2, float, float)`, `EnemyStatus.IsKnockedBack`(Task 1), `EnemyHealth.CurrentHealth`, `ChaseContactEnemyController`/`ApproachAndShootEnemyController`/`KrabEnemy`(기존 클래스).
- Produces: `EnemyKnockback.Apply(EnemyHealth target, Vector2 attackDirection)` — Task 3~6이 각 히트 지점에서 호출.

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/EnemyKnockbackTests.cs` 새로 작성:

```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>추적형 적만 넉백 대상이 되는지, 죽었거나 방향이 없으면 무시되는지.</summary>
public sealed class EnemyKnockbackTests
{
    private GameObject enemy;
    private EnemyHealth health;

    [SetUp]
    public void SetUp()
    {
        enemy = new GameObject("enemy");
        health = enemy.AddComponent<EnemyHealth>();
        health.Configure(100);
    }

    [TearDown]
    public void TearDown()
    {
        if (enemy != null)
        {
            Object.DestroyImmediate(enemy);
        }
    }

    [Test]
    public void ChaseEnemyGetsKnockedBack()
    {
        enemy.AddComponent<ChaseContactEnemyController>();

        EnemyKnockback.Apply(health, Vector2.right);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);
    }

    [Test]
    public void EnemyWithoutAChaseControllerIsIgnored()
    {
        EnemyKnockback.Apply(health, Vector2.right);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }

    [Test]
    public void DeadEnemyIsIgnored()
    {
        enemy.AddComponent<ChaseContactEnemyController>();
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        health.TakeDamage(1000);

        EnemyKnockback.Apply(health, Vector2.right);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }

    [Test]
    public void ZeroDirectionIsIgnored()
    {
        enemy.AddComponent<ChaseContactEnemyController>();

        EnemyKnockback.Apply(health, Vector2.zero);

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }
}
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'EnemyKnockbackTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemyknockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemyknockback-red.log'
```
Expected: `EnemyKnockback` 타입이 없어서 컴파일 에러로 FAIL.

### Step 3: 최소 구현

`Assets/Scripts/Combat/EnemyKnockback.cs` 새로 작성:

```csharp
using UnityEngine;

/// <summary>
/// 플레이어 직접 타격이 적을 넉백시킬지 판정하는 단일 진입점.
/// 추적형 컨트롤러(체이스·접근사격·크랩)가 있는 살아있는 적만 대상이다 — 보스와
/// 고정형 사수는 이동 로직 자체가 없어 밀려나도 제자리로 못 돌아오므로 제외한다.
/// </summary>
public static class EnemyKnockback
{
    private const float Distance = 0.3f;
    private const float Duration = 0.12f;

    public static void Apply(EnemyHealth target, Vector2 attackDirection)
    {
        if (target == null || target.CurrentHealth <= 0 || attackDirection == Vector2.zero)
        {
            return;
        }

        if (!IsEligible(target.gameObject))
        {
            return;
        }

        EnemyStatus.EnsureOn(target).ApplyKnockback(-attackDirection, Distance, Duration);
    }

    private static bool IsEligible(GameObject enemy)
    {
        return enemy.GetComponent<ChaseContactEnemyController>() != null
            || enemy.GetComponent<ApproachAndShootEnemyController>() != null
            || enemy.GetComponent<KrabEnemy>() != null;
    }
}
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'EnemyKnockbackTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemyknockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\enemyknockback-green.log'
```
Expected: 4개 테스트 모두 PASS.

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/EnemyKnockback.cs Assets/Tests/Editor/EnemyKnockbackTests.cs
git commit -m "feat: EnemyKnockback 대상 판정 헬퍼 추가"
```

---

## Task 3: 근접 판정(MeleeStrike)에 넉백 연결

**Files:**
- Modify: `Assets/Scripts/Combat/Deliveries/MeleeDeliveries.cs:42-46`
- Test: `Assets/Tests/Editor/MeleeStrikeTests.cs` (신규)

**Interfaces:**
- Consumes: `EnemyKnockback.Apply(EnemyHealth, Vector2)`(Task 2), 기존 `MeleeStrike.Execute(DeliveryContext, float, float)`, `DeliveryContext`, `WeaponFactory.CreateWeapon(...)`, `WeaponLoadout`, `DeliverySpec`, `ParamSet`(모두 기존 코드).
- Produces: 없음(이 기능의 마지막 소비자 — swing/thrust/spin 세 궤도가 전부 `MeleeStrike.Execute`를 공유하므로 이 한 곳만 고치면 셋 다 적용됨).

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/MeleeStrikeTests.cs` 새로 작성:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>근접 판정(MeleeStrike) — 맞은 적이 공격 방향의 반대로 넉백되는지.</summary>
public sealed class MeleeStrikeTests
{
    private GameObject owner;
    private WeaponDefinition weapon;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("player");
        weapon = WeaponFactory.CreateWeapon(
            "test_sword", "시험 검", WeaponCategory.Melee, WeaponType.Sword,
            damage: 10, interval: 1f, reach: 2f, radius: 0.25f, arc: 360f,
            speed: 0f, lifetime: 0f, sprite: null, color: Color.white);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (EnemyHealth leftover in Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(leftover.gameObject);
        }

        Object.DestroyImmediate(owner);
        Object.DestroyImmediate(weapon);
    }

    private DeliveryContext ContextFacing(Vector2 direction)
    {
        var loadout = new WeaponLoadout(
            weapon, new DeliverySpec("swing", ParamSet.Empty), WeaponLoadout.NoEffects);
        return new DeliveryContext(null, owner, Vector2.zero, direction, loadout, null, null);
    }

    private static EnemyHealth MakeChaseEnemy(Vector2 position)
    {
        var enemyObject = new GameObject("enemy");
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(100);
        enemyObject.AddComponent<ChaseContactEnemyController>();

        Physics2D.SyncTransforms();
        return health;
    }

    [Test]
    public void HitEnemyIsKnockedBackOppositeTheAttackDirection()
    {
        EnemyHealth enemy = MakeChaseEnemy(new Vector2(1f, 0f));

        MeleeStrike.Execute(ContextFacing(Vector2.right));

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);
    }

    [Test]
    public void MissedEnemyOutsideReachIsNotKnockedBack()
    {
        EnemyHealth enemy = MakeChaseEnemy(new Vector2(10f, 0f));

        MeleeStrike.Execute(ContextFacing(Vector2.right));

        Assert.That(enemy.GetComponent<EnemyStatus>(), Is.Null);
    }
}
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'MeleeStrikeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\meleestrike-knockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\meleestrike-knockback-red.log'
```
Expected: `HitEnemyIsKnockedBackOppositeTheAttackDirection`이 `status`가 null이라 FAIL(아직 연결 안 함). `MissedEnemyOutsideReachIsNotKnockedBack`은 이미 PASS일 수 있음.

### Step 3: 최소 구현

`Assets/Scripts/Combat/Deliveries/MeleeDeliveries.cs:42-46`을 다음처럼 바꾼다(기존 `hits++;` 부터 `NotifyHit` 줄까지):

```csharp
            hits++;
            enemy.TakeDamage(weapon.Damage);
            EnemyKnockback.Apply(enemy, context.Direction);
            // 죽음 판정은 피해 적용 후 — on_kill이 여기서 갈린다
            context.Runner?.NotifyHit(enemy, enemy.transform.position, context.Direction);
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'MeleeStrikeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\meleestrike-knockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\meleestrike-knockback-green.log'
```
Expected: 두 테스트 모두 PASS.

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/Deliveries/MeleeDeliveries.cs Assets/Tests/Editor/MeleeStrikeTests.cs
git commit -m "feat: 근접 타격에 넉백 연결"
```

---

## Task 4: 검 발사체(SwordProjectile)에 넉백 연결

**Files:**
- Modify: `Assets/Scripts/Combat/SwordProjectile.cs:62-79`
- Test: `Assets/Tests/Editor/SwordProjectileTests.cs`

**Interfaces:**
- Consumes: `EnemyKnockback.Apply(EnemyHealth, Vector2)`(Task 2), 기존 `SwordProjectile.direction` 필드.
- Produces: 없음.

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/SwordProjectileTests.cs`의 `TryHit_EnemyTakesDamageOnlyOnce` 테스트 다음에 추가:

```csharp
    [UnityTest]
    public IEnumerator TryHit_AppliesKnockbackOppositeTheTravelDirection()
    {
        yield return new EnterPlayMode();

        SwordProjectile projectile = CreateProjectile();
        projectile.Initialize(Vector2.right, 5, 8f, 720f, 4f, null);
        var enemy = new GameObject("Enemy");
        Collider2D enemyCollider = enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<ChaseContactEnemyController>();

        Assert.That(projectile.TryHit(enemyCollider), Is.True);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'SwordProjectileTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\swordprojectile-knockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\swordprojectile-knockback-red.log'
```
Expected: `status`가 null이라 새 테스트만 FAIL, 기존 테스트는 PASS.

### Step 3: 최소 구현

`Assets/Scripts/Combat/SwordProjectile.cs:74-77`을 다음처럼 바꾼다:

```csharp
        consumed = true;
        enemyHealth.TakeDamage(damage);
        EnemyKnockback.Apply(enemyHealth, direction);
        Destroy(gameObject);
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'SwordProjectileTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\swordprojectile-knockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\swordprojectile-knockback-green.log'
```
Expected: 전체 PASS.

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/SwordProjectile.cs Assets/Tests/Editor/SwordProjectileTests.cs
git commit -m "feat: 검 발사체 타격에 넉백 연결"
```

---

## Task 5: 일반 무기 발사체(WeaponProjectile)에 넉백 연결

**Files:**
- Modify: `Assets/Scripts/Combat/WeaponProjectile.cs:230-232`
- Test: `Assets/Tests/Editor/WeaponProjectilePierceTests.cs`

**Interfaces:**
- Consumes: `EnemyKnockback.Apply(EnemyHealth, Vector2)`(Task 2), 기존 `WeaponProjectile.direction` 필드(유도·도탄으로 꺾인 현재 진행 방향).
- Produces: 없음.

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/WeaponProjectilePierceTests.cs`의 `PlainProjectileStopsAtTheFirstHit` 테스트 다음에 추가:

```csharp
    [Test]
    public void HittingAChaseEnemyAppliesKnockbackOppositeTheTravelDirection()
    {
        ExpectEditModeDestroyError();
        WeaponProjectile projectile = Spawn();
        var enemyObject = new GameObject("enemy");
        var collider = enemyObject.AddComponent<CircleCollider2D>();
        EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
        enemyHealth.Configure(100);
        enemyObject.AddComponent<ChaseContactEnemyController>();

        Assert.That(projectile.TryHit(collider), Is.True);

        EnemyStatus status = enemyObject.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.DestroyImmediate(enemyObject);
    }
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'WeaponProjectilePierceTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\weaponprojectile-knockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\weaponprojectile-knockback-red.log'
```
Expected: 새 테스트만 `status`가 null이라 FAIL.

### Step 3: 최소 구현

`Assets/Scripts/Combat/WeaponProjectile.cs:230-232`을 다음처럼 바꾼다:

```csharp
        health.TakeDamage(definition.Damage);
        EnemyKnockback.Apply(health, direction);
        // 죽음 판정은 피해 적용 후 — on_kill이 여기서 갈린다
        effectRunner?.NotifyHit(health, transform.position, direction);
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'WeaponProjectilePierceTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\weaponprojectile-knockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\weaponprojectile-knockback-green.log'
```
Expected: 전체 PASS.

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/WeaponProjectile.cs Assets/Tests/Editor/WeaponProjectilePierceTests.cs
git commit -m "feat: 일반 무기 발사체 타격에 넉백 연결"
```

---

## Task 6: 도끼(AxeSwing)에 공격 방향 전달 + 넉백 연결

**Files:**
- Modify: `Assets/Scripts/Combat/AxeSwing.cs:14-23` (Initialize), `:57-73` (TryHit)
- Modify: `Assets/Scripts/Combat/PlayerAxeAttacker.cs:83-96` (SpawnSwing)
- Test: `Assets/Tests/Editor/AxeSwingTests.cs`

**Interfaces:**
- Consumes: `EnemyKnockback.Apply(EnemyHealth, Vector2)`(Task 2).
- Produces: `AxeSwing.Initialize(GameObject, int, float, Vector2 = default)` — 4번째 매개변수가 옵션이라 기존 3-인자 호출부(`AxeSwingTests.cs`, `AxeSwingPhysicsPlayModeTests.cs`)는 그대로 컴파일된다.

### Step 1: 실패하는 테스트 작성

`Assets/Tests/Editor/AxeSwingTests.cs`의 `TryHit_SameEnemyTakesTenDamageOnlyOnce` 테스트 다음에 추가:

```csharp
    [UnityTest]
    public IEnumerator TryHit_AppliesKnockbackOppositeTheSwingDirection()
    {
        yield return new EnterPlayMode();
        AxeSwing swing = CreateSwing();
        swing.Initialize(null, 10, 0.45f, Vector2.right);
        var enemy = new GameObject("Enemy");
        Collider2D collider = enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<ChaseContactEnemyController>();

        Assert.That(swing.TryHit(collider), Is.True);

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status.IsKnockedBack, Is.True);

        Object.Destroy(swing.gameObject);
        Object.Destroy(enemy);
        yield return new ExitPlayMode();
    }
```

### Step 2: 테스트가 실패하는지 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'AxeSwingTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswing-knockback-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswing-knockback-red.log'
```
Expected: `Initialize`가 4-인자를 받지 않아 컴파일 에러로 FAIL.

### Step 3: 최소 구현

`Assets/Scripts/Combat/AxeSwing.cs`에서 필드와 `Initialize`(14-23행)를 다음처럼 바꾼다:

```csharp
    private readonly HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
    private GameObject owner;
    private int damage;
    private float duration;
    private float elapsedTime;
    private bool initialized;
    private bool completed;
    private Vector2 attackDirection;

    public void Initialize(
        GameObject newOwner, int newDamage, float newDuration, Vector2 newDirection = default)
    {
        owner = newOwner;
        damage = Mathf.Max(0, newDamage);
        duration = Mathf.Max(0.0001f, newDuration);
        attackDirection = newDirection;
        elapsedTime = 0f;
        completed = false;
        initialized = true;
        damagedEnemies.Clear();
    }
```

`TryHit`(57-73행)에서 `enemyHealth.TakeDamage(damage);` 다음 줄에 추가:

```csharp
        damagedEnemies.Add(enemyHealth);
        enemyHealth.TakeDamage(damage);
        EnemyKnockback.Apply(enemyHealth, attackDirection);
        return true;
```

`Assets/Scripts/Combat/PlayerAxeAttacker.cs:96`의 `swing.Initialize(gameObject, damage, swingDuration);`을 다음처럼 바꾼다:

```csharp
        swing.Initialize(gameObject, damage, swingDuration, direction);
```

### Step 4: 테스트 통과 확인

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'AxeSwingTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswing-knockback-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswing-knockback-green.log'
```
Expected: 전체 PASS(기존 3-인자 `Initialize` 호출 테스트 포함).

같은 파일을 쓰는 `Assets/Tests/PlayMode/AxeSwingPhysicsPlayModeTests.cs`와 `Assets/Tests/Editor/PlayerAxeAttackerTests.cs`도 컴파일이 깨지지 않았는지 함께 확인한다:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform PlayMode -testFilter 'AxeSwingPhysicsPlayModeTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswingphysics-check.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\axeswingphysics-check.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'PlayerAxeAttackerTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\playeraxeattacker-check.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\playeraxeattacker-check.log'
```
Expected: 둘 다 기존과 동일하게 PASS(동작 변경 없음, 시그니처 호환성만 확인).

### Step 5: 커밋

```bash
git add Assets/Scripts/Combat/AxeSwing.cs Assets/Scripts/Combat/PlayerAxeAttacker.cs Assets/Tests/Editor/AxeSwingTests.cs
git commit -m "feat: 도끼 타격에 공격 방향 전달 및 넉백 연결"
```

---

## 완료 조건

- 6개 태스크 모두 커밋됨.
- 새로 추가/수정한 테스트 클래스(`EnemyStatusTests`, `EnemyKnockbackTests`, `MeleeStrikeTests`, `SwordProjectileTests`, `WeaponProjectilePierceTests`, `AxeSwingTests`)가 각각의 `-testFilter`로 전부 PASS.
- 전체 테스트 스위트는 실행하지 않는다(정책상 범위 밖).
