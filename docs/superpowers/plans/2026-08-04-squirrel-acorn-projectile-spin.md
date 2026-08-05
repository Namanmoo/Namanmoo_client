# Squirrel Acorn Projectile Spin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 다람쥐가 발사하는 도토리 투사체만 이동 중 Z축으로 초당 720도 회전하게 한다.

**Architecture:** 공용 `EnemyProjectile`에 기본값 0인 선택적 회전 속도를 추가한다. 기존 초기화 오버로드는 회전 속도 0을 전달해 기존 투사체 동작을 보존하고, `ApproachAndShootEnemyController`만 720도를 전달한다.

**Tech Stack:** Unity, C#, NUnit, Unity Test Framework

## Global Constraints

- 다람쥐와 Krab의 체력, 공격력, 이동 속도, 투사체 속도 및 기타 기존 스탯을 변경하지 않는다.
- 회전은 시각적 Z축 회전에만 적용한다.
- 기존 투사체 초기화 경로의 기본 회전 속도는 0이다.
- 사용자가 요청하지 않았으므로 Git 커밋을 만들지 않는다.

---

### Task 1: 선택적 투사체 회전

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyProjectile.cs`
- Modify: `Assets/Tests/Editor/EnemyProjectileTests.cs`

**Interfaces:**
- Produces: `public float RotationSpeed { get; }`
- Produces: `Initialize(..., float collisionRadius, float rotationSpeed = 0f)`
- Behavior: `Advance(float deltaTime)`가 이동과 함께 Z축을 회전

- [ ] **Step 1: 회전과 기본값을 검증하는 실패 테스트 작성**

`EnemyProjectileTests`에 다음 두 동작을 추가한다.

```csharp
[Test]
public void Advance_RotatesByConfiguredDegreesPerSecond()
{
    var projectileObject = new GameObject("Spinning Projectile");
    try
    {
        EnemyProjectile projectile =
            projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            null, null, Vector2.right, 1, 0f, 2f, 0.1f, 720f);

        projectile.Advance(0.25f);

        Assert.That(projectile.RotationSpeed, Is.EqualTo(720f));
        Assert.That(
            Mathf.DeltaAngle(projectile.transform.eulerAngles.z, 180f),
            Is.Zero.Within(0.001f));
    }
    finally
    {
        Object.DestroyImmediate(projectileObject);
    }
}

[Test]
public void Initialize_WithoutRotationSpeedDoesNotRotate()
{
    var projectileObject = new GameObject("Static Projectile");
    try
    {
        EnemyProjectile projectile =
            projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            null, null, Vector2.right, 1, 0f, 2f, 0.1f);

        projectile.Advance(0.25f);

        Assert.That(projectile.RotationSpeed, Is.Zero);
        Assert.That(projectile.transform.eulerAngles.z, Is.Zero.Within(0.001f));
    }
    finally
    {
        Object.DestroyImmediate(projectileObject);
    }
}
```

- [ ] **Step 2: RED 확인**

Unity EditMode에서 `EnemyProjectileTests`를 실행한다.

Expected: 새 `Initialize` 인자와 `RotationSpeed`가 없어 컴파일 실패.

- [ ] **Step 3: 최소 회전 구현**

`EnemyProjectile`에 상태와 읽기 전용 프로퍼티를 추가한다.

```csharp
private float rotationSpeed;
public float RotationSpeed => rotationSpeed;
```

스프라이트를 받는 초기화 오버로드의 마지막 인자를 다음처럼 확장한다.

```csharp
float collisionRadius,
float newRotationSpeed = 0f
```

초기화 시 값을 저장하고 `Advance`에서 회전한다.

```csharp
rotationSpeed = newRotationSpeed;
transform.Rotate(0f, 0f, rotationSpeed * deltaTime);
```

기존 첫 번째 오버로드는 변경된 오버로드를 호출할 때 회전 속도 0을 사용한다.

- [ ] **Step 4: GREEN 확인**

Unity EditMode에서 `EnemyProjectileTests` 전체를 실행한다.

Expected: 모든 테스트 PASS.

### Task 2: 다람쥐 발사 경로 연결

**Files:**
- Modify: `Assets/Scripts/Enemies/ApproachAndShootEnemyController.cs`
- Modify: `Assets/Tests/Editor/ApproachAndShootEnemyControllerTests.cs`

**Interfaces:**
- Consumes: `EnemyProjectile.Initialize(..., float collisionRadius, float rotationSpeed)`
- Produces: 다람쥐 도토리 회전 속도 `ProjectileRotationSpeed = 720f`

- [ ] **Step 1: 다람쥐 투사체 회전 옵션 실패 테스트 작성**

기존 `TryAttack_EnforcesIntervalAndInitializesProjectileFromDefinition`의 각 투사체 검증에 다음 assertion을 추가한다.

```csharp
Assert.That(projectile.RotationSpeed, Is.EqualTo(720f));
```

- [ ] **Step 2: RED 확인**

Unity EditMode에서 `ApproachAndShootEnemyControllerTests`를 실행한다.

Expected: 생성된 투사체의 회전 속도가 기본값 0이어서 FAIL.

- [ ] **Step 3: 다람쥐 발사 시에만 720도 전달**

`ApproachAndShootEnemyController`에 다음 상수를 추가한다.

```csharp
private const float ProjectileRotationSpeed = 720f;
```

`projectile.Initialize` 호출의 마지막 인자로 상수를 전달한다.

```csharp
definition.ProjectileRadius,
ProjectileRotationSpeed);
```

- [ ] **Step 4: 관련 회귀 검증**

Unity EditMode에서 다음 테스트를 실행한다.

- `EnemyProjectileTests`
- `ApproachAndShootEnemyControllerTests`
- `DungeonSceneBuilderTests`

Expected: 모두 PASS. EnemyDefinition 및 Krab/다람쥐 스탯 에셋 diff 없음.

- [ ] **Step 5: 변경 범위 검증**

```powershell
git diff --check
git diff -- Assets/Scripts/Enemies/EnemyProjectile.cs Assets/Scripts/Enemies/ApproachAndShootEnemyController.cs Assets/Tests/Editor/EnemyProjectileTests.cs Assets/Tests/Editor/ApproachAndShootEnemyControllerTests.cs
git status --short
```

Expected: 회전 동작과 테스트 파일만 변경되고 스탯 에셋 변경 없음.
