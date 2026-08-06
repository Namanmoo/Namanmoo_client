# 술탄 보스 투사체 방향 회전 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `SultanBossController`의 `AimedShotPattern`/`EightWayShotPattern`이 발사하는 투사체 스프라이트가 `flipX` 좌우 반전이 아니라 실제 방향 각도로 회전하도록 바꾼다.

**Architecture:** `FireEnemyProjectile()`이 공유 호출하는 `GetProjectileOrientation(direction, out angle, out flipX)`(세로만 ±90도 스냅 + flipX)를 `GetProjectileRotationAngle(direction)`(순수 `atan2` 회전, flipX 없음)로 교체한다. `EnemyProjectile.Initialize`는 이미 임의의 `initialRotation`을 받으므로 그쪽은 수정하지 않는다.

**Tech Stack:** Unity 6000.5.5f1, C# (전역 네임스페이스, MonoBehaviour), NUnit + Unity Test Framework(EditMode).

## Global Constraints

- 플레이어·몬스터·보스·무기의 기존 밸런스 수치(체력, 공격력, 이동속도, 사거리, 공격 간격, 투사체 속도 등)는 변경하지 않는다.
- 이번 작업에서 수정한 파일과 직접 관련된 테스트만 실행한다. 전체 테스트 스위트나 관련 없는 회귀 테스트는 실행하지 않는다.
- Unity 테스트는 항상 `-testFilter`로 좁혀서 실행한다(`-runTests` 단독 실행 금지).
- 좌측 방향 회전 시 스프라이트가 위아래로 뒤집힌 것처럼 보여도 별도 `flipY` 보정을 하지 않는다(설계 문서에서 사용자가 확인한 사항).
- 이 변경은 `AimedShotPattern`과 `EightWayShotPattern` 모두에 동일하게 적용된다(둘 다 `FireEnemyProjectile()`을 공유).
- Unity 테스트 실행 커맨드 형식(이 저장소의 기존 관례):
  ```powershell
  & 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter '<ClassName>' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\<label>.log'
  ```

---

## Task 1: flipX 기반 방향 계산을 순수 회전으로 교체

**Files:**
- Modify: `Assets/Scripts/Enemies/SultanBossController.cs:316-365` (`FireEnemyProjectile`, `GetProjectileOrientation` → `GetProjectileRotationAngle`)
- Test: `Assets/Tests/Editor/SultanBossProjectileOrientationTests.cs` (신규)

**Interfaces:**
- Consumes: 없음(기존 `EnemyProjectile.Initialize(GameObject, Sprite, Vector2, int, float, float, float, float, float)`의 `initialRotation` 인자는 그대로 사용).
- Produces: `SultanBossController.GetProjectileRotationAngle(Vector2 direction)` (private static, `float` 반환) — 리플렉션으로만 테스트에서 접근한다. 이 시그니처를 바꾸면 아래 테스트도 함께 바꿔야 한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/Tests/Editor/SultanBossProjectileOrientationTests.cs` 파일을 새로 만든다.

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SultanBossProjectileOrientationTests
{
    private static float InvokeGetProjectileRotationAngle(Vector2 direction)
    {
        MethodInfo method = typeof(SultanBossController).GetMethod(
            "GetProjectileRotationAngle",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (float)method.Invoke(null, new object[] { direction });
    }

    [Test]
    public void RightDirection_ReturnsZeroDegrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.right);
        Assert.That(Mathf.DeltaAngle(angle, 0f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void UpDirection_Returns90Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.up);
        Assert.That(Mathf.DeltaAngle(angle, 90f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void DownDirection_ReturnsMinus90Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.down);
        Assert.That(Mathf.DeltaAngle(angle, -90f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void LeftDirection_Returns180Degrees()
    {
        float angle = InvokeGetProjectileRotationAngle(Vector2.left);
        Assert.That(Mathf.DeltaAngle(angle, 180f), Is.Zero.Within(0.01f));
    }

    [Test]
    public void DiagonalUpRightDirection_Returns45Degrees()
    {
        Vector2 direction = new Vector2(1f, 1f).normalized;
        float angle = InvokeGetProjectileRotationAngle(direction);
        Assert.That(Mathf.DeltaAngle(angle, 45f), Is.Zero.Within(0.01f));
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Run:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'SultanBossProjectileOrientationTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\sultanboss-projectile-rotation-red.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\sultanboss-projectile-rotation-red.log'
```
Expected: FAIL — `GetProjectileRotationAngle`가 아직 없어서 `GetMethod`가 `null`을 돌려주고, `null.Invoke(...)`에서 `NullReferenceException`이 발생해 5개 테스트 모두 실패한다.

- [ ] **Step 3: `FireEnemyProjectile`/`GetProjectileOrientation`을 `GetProjectileRotationAngle`로 교체**

`Assets/Scripts/Enemies/SultanBossController.cs`의 316~365번째 줄(현재 `FireEnemyProjectile`부터 `GetProjectileOrientation` 끝까지)을 다음으로 통째로 바꾼다.

```csharp
    private void FireEnemyProjectile(Vector2 direction)
    {
        EnemyDefinition source = definition.WoodTowerDefinition;
        if (source == null || direction == Vector2.zero)
        {
            return;
        }

        float angle = GetProjectileRotationAngle(direction);

        var projectileObject = new GameObject("Sultan Bullet");
        projectileObject.transform.position = transform.position;
        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            gameObject,
            source.ProjectileSprite,
            direction,
            source.AttackDamage,
            source.ProjectileSpeed,
            source.ProjectileLifetime,
            source.ProjectileRadius,
            0f,
            angle);
    }

    /// <summary>
    /// 탄환 스프라이트는 0도일 때 오른쪽을 바라본다. 방향 벡터의 각도를 그대로
    /// 회전에 적용해, flipX 반전 없이 실제 방향을 향하도록 한다.
    /// </summary>
    private static float GetProjectileRotationAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
```

이 변경으로 `SpriteRenderer projectileVisual`을 가져와 `flipX`를 대입하던 코드가 통째로 사라진다 — 더 이상 필요 없다.

- [ ] **Step 4: 테스트 실행해서 통과 확인**

Run:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\myong\NaManMoo\Namanmoo_client' -runTests -testPlatform EditMode -testFilter 'SultanBossProjectileOrientationTests' -testResults 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\sultanboss-projectile-rotation-green.xml' -logFile 'C:\Users\myong\NaManMoo\Namanmoo_client\Artifacts\sultanboss-projectile-rotation-green.log'
```
Expected: 5개 테스트 모두 PASS.

- [ ] **Step 5: 컴파일 확인**

같은 테스트 실행 로그(`sultanboss-projectile-rotation-green.log`)에 C# 컴파일 에러가 없는지 확인한다(경고는 무방). Step 4가 PASS로 끝났다면 이미 컴파일이 성공했다는 뜻이므로 별도 스텝은 로그 확인만으로 충분하다.

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Enemies/SultanBossController.cs Assets/Tests/Editor/SultanBossProjectileOrientationTests.cs
git commit -m "$(cat <<'EOF'
feat: 술탄 보스 투사체 방향을 flipX 대신 실제 회전으로 표현

AimedShotPattern/EightWayShotPattern이 공유하는 FireEnemyProjectile의
방향 계산을 GetProjectileOrientation(각도 스냅+flipX)에서
GetProjectileRotationAngle(atan2 순수 회전)로 교체한다.
EOF
)"
```
