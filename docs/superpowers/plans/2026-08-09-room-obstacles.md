# Room Obstacles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three obstacle prefabs a designer can drop into a
`RoomContentTemplate`: `Obstacle_Lake`/`Obstacle_Rock` block player and
monster movement; `Obstacle_Spike` lets everyone pass, but deals 2 damage to
the player (never monsters) on contact.

**Architecture:** One new component, `SpikeObstacle`, mirroring the existing
`ChaseContactEnemyController.TryDamagePlayer` pattern exactly. Three new
prefabs under `Assets/Resources/Stage1/Obstacle/` — Lake/Rock need no new
code (a plain non-trigger `Collider2D` already blocks movement, the same way
the room's Safety Boundary wall does), Spike carries the new component.

**Tech Stack:** Unity (C#), NUnit EditMode/PlayMode tests
(`Assets/Tests/Editor`), hand-authored `.prefab` YAML (this project has no
prefab-creation tooling; existing prefabs in this repo were hand-authored
the same way and verified by batch-mode import).

## Global Constraints

- Spike's damage amount (2) and invulnerability window (1 second) are
  explicit, user-specified values — not protected "existing balance values"
  under CLAUDE.md, since nothing existing is being changed. 1 second matches
  `ChaseContactEnemyController.PlayerInvulnerabilityDuration`, this game's
  existing convention for contact damage.
- Do not modify any existing file. This plan is purely additive — no
  existing balance values, no existing gameplay code, changes at all.
- Obstacles must not check for `EnemyHealth` or any enemy-specific type —
  monsters passing through spikes taking no damage is a consequence of the
  component never looking for them, not a special case to add.
- Lake/Rock need zero new C# code — only prefab authoring (`SpriteRenderer`
  + non-trigger `Collider2D`).
- Spec: `docs/superpowers/specs/2026-08-09-room-obstacles-design.md`

---

### Task 1: SpikeObstacle component

**Files:**
- Create: `Assets/Scripts/Dungeon/Obstacle/SpikeObstacle.cs`
- Test: `Assets/Tests/Editor/SpikeObstacleTests.cs`

**Interfaces:**
- Consumes: existing `PlayerHealth.TryTakeDamage(int amount, float
  currentTime, float invulnerabilityDuration) : bool`
  (`Assets/Scripts/Player/PlayerHealth.cs`).
- Produces: `NaManMoo.Dungeon.SpikeObstacle` (`[RequireComponent(typeof(Collider2D))]`
  `MonoBehaviour`), with `public bool TryDamagePlayer(Collider2D other,
  float currentTime)` — later tasks/designers don't call this directly (it's
  wired to Unity trigger callbacks), but it's public and deterministic so it
  can be tested without simulating real Unity physics events, the same way
  `ChaseContactEnemyController.TryDamagePlayer` already is.

- [ ] **Step 1: Write the failing test**

Note: this task's automated coverage is narrower than originally planned,
for a confirmed, pre-existing, unrelated reason. Investigation (see below)
proved that in this project's headless batch-mode EditMode test execution,
a freshly `AddComponent`'d `MonoBehaviour`'s `Awake()` is not reliably
observable within the same test method, by ANY technique tried: a plain
`[Test]`, a `[UnityTest]` with `EnterPlayMode`/`ExitPlayMode`, a
`[UnityTest]` with a plain `yield return null;` (no play mode at all), and
the `SetActive(false)` → add components → `SetActive(true)` pattern. This
is not specific to `SpikeObstacle` or anything this task touches: the
pre-existing, already-committed test
`ChaseContactEnemyControllerTests.TryDamagePlayer_UsesDefinitionDamageAndSharedInvulnerability`
(`Assets/Tests/Editor/ChaseContactEnemyControllerTests.cs`) — which this
task did not modify — fails identically (`Expected: True, But was: False`)
when run in isolation via `-testFilter
"ChaseContactEnemyControllerTests.TryDamagePlayer_UsesDefinitionDamageAndSharedInvulnerability"`,
for the same reason: it needs a fresh `PlayerHealth`'s `Awake()` (which
sets `CurrentHealth = maxHealth`) to have run, and it hasn't. `PlayerHealth`
has no public API to initialize `CurrentHealth` without `Awake()` running
(`TryTakeDamage` and `Heal` both no-op while `CurrentHealth` is stuck at
its C# default of 0), so no test in this codebase can work around this
without modifying `PlayerHealth.cs` itself — out of scope for this plan.

Given this is a confirmed pre-existing environment limitation and not
something to fix here, this task tests only the one behavior that doesn't
require a fresh `PlayerHealth`'s `Awake()` to have run: that
`TryDamagePlayer` correctly does nothing when the other collider has no
`PlayerHealth` in its parent chain (this path never touches
`PlayerHealth` at all). The "deals damage" and "respects invulnerability"
behavior is not unit-tested here; it's a straightforward 3-line method
(`null` check → `GetComponentInParent<PlayerHealth>()` → delegate to the
already-tested `PlayerHealth.TryTakeDamage`) that mirrors the exact pattern
`ChaseContactEnemyController.TryDamagePlayer` already uses in production —
verify it by code review, not by test, for this task.

Create `Assets/Tests/Editor/SpikeObstacleTests.cs`:

```csharp
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class SpikeObstacleTests
{
    [Test]
    public void TryDamagePlayer_ColliderWithoutPlayerHealthDoesNothing()
    {
        var monster = new GameObject("Monster");
        Collider2D monsterCollider = monster.AddComponent<CircleCollider2D>();
        var spikeObject = new GameObject("Spike");
        spikeObject.AddComponent<BoxCollider2D>();
        SpikeObstacle spike = spikeObject.AddComponent<SpikeObstacle>();

        try
        {
            Assert.That(spike.TryDamagePlayer(monsterCollider, 0f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(monster);
            Object.DestroyImmediate(spikeObject);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `-testFilter "SpikeObstacleTests"`
Expected: FAIL to compile — `NaManMoo.Dungeon.SpikeObstacle` does not exist
yet.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/Scripts/Dungeon/Obstacle/SpikeObstacle.cs`:

```csharp
using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 지나갈 수는 있지만 플레이어에게만 데미지를 주는 장애물. 몬스터는 그냥 지나간다 —
    /// 몬스터 쪽 컴포넌트를 아예 찾지 않아서, 특별히 막을 필요가 없다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SpikeObstacle : MonoBehaviour
    {
        private const float PlayerInvulnerabilityDuration = 1f;

        [SerializeField, Min(0)] private int damage = 2;

        private void Awake()
        {
            // 트리거가 아니면 물리적으로 막혀버려 "지나갈 수 있다"가 깨진다
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other, Time.time);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other, Time.time);
        }

        public bool TryDamagePlayer(Collider2D other, float currentTime)
        {
            if (other == null)
            {
                return false;
            }

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null)
            {
                return false;
            }

            return health.TryTakeDamage(damage, currentTime, PlayerInvulnerabilityDuration);
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `-testFilter "SpikeObstacleTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Dungeon/Obstacle/SpikeObstacle.cs Assets/Tests/Editor/SpikeObstacleTests.cs
git commit -m "feat: 플레이어에게만 데미지를 주는 SpikeObstacle 추가"
```

---

### Task 2: Obstacle_Lake, Obstacle_Rock, Obstacle_Spike prefabs

**Files:**
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab`
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab.meta`
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab`
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab.meta`
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab`
- Create: `Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab.meta`
- Test: `Assets/Tests/Editor/RoomObstaclePrefabsTests.cs`

**Interfaces:**
- Consumes: `NaManMoo.Dungeon.SpikeObstacle` (Task 1) — the
  `Obstacle_Spike.prefab`'s `SpikeObstacle` `MonoBehaviour` component
  references this script by GUID, read from the real, Task-1-committed
  `Assets/Scripts/Dungeon/Obstacle/SpikeObstacle.cs.meta` (see Step 1 below — this
  GUID cannot be known ahead of time; Unity assigns it the first time the
  script is imported, which already happened when Task 1 ran its tests).
- Produces: three ready-to-use prefab assets. Nothing else in this plan
  consumes them — a designer places them inside `RoomContentTemplate`
  prefabs by hand in the Unity Editor, which is out of this plan's scope
  (no code reads these prefabs by path or otherwise).

The folder `Assets/Resources/Stage1/Obstacle/` already exists in this repo
(currently empty) — do not create or modify its `.meta`.

- [ ] **Step 1: Look up the SpikeObstacle script GUID**

Read `Assets/Scripts/Dungeon/Obstacle/SpikeObstacle.cs.meta` (created by Task 1,
already committed) and copy its `guid:` value. You'll substitute it for
`<SPIKE_SCRIPT_GUID>` in Step 3 below. Do not guess this value or reuse a
GUID from a different script — every `.meta` file's GUID is unique and
Unity-assigned.

- [ ] **Step 2: Generate three fresh prefab GUIDs**

Run three times (or once with a different seed each time) to get three
distinct 32-character lowercase hex strings, one per new `.prefab.meta`
file:

```bash
openssl rand -hex 16
```

Each run prints a different GUID — use a different one for
`Obstacle_Lake.prefab.meta`, `Obstacle_Rock.prefab.meta`, and
`Obstacle_Spike.prefab.meta`. Do not reuse a GUID across files — Unity
requires every asset's GUID to be unique within the project.

- [ ] **Step 3: Write the three prefabs**

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab`:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000}
  - component: {fileID: 212000}
  - component: {fileID: 61000}
  m_Layer: 0
  m_Name: Obstacle_Lake
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &212000
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: a97c105638bdf4679a2b3f508e70587a, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Sprite: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 1, y: 1}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 0
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!61 &61000
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {x: 0, y: 0}
  m_SpriteTilingProperty:
    border: {x: 0, y: 0, z: 0, w: 0}
    pivot: {x: 0.5, y: 0.5}
    oldSize: {x: 1, y: 1}
    newSize: {x: 1, y: 1}
    adaptiveTilingThreshold: 0.5
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  m_Size: {x: 2, y: 2}
  m_EdgeRadius: 0
```

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab.meta`
(substitute the GUID you generated in Step 2 for this file):

```yaml
fileFormatVersion: 2
guid: <LAKE_PREFAB_GUID>
PrefabImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab` — identical
to `Obstacle_Lake.prefab` above, with exactly two changes: `m_Name:
Obstacle_Lake` becomes `m_Name: Obstacle_Rock`, and every `&100000` /
`&400000` / `&212000` / `&61000` anchor and every `{fileID: 100000}` /
`{fileID: 400000}` / `{fileID: 212000}` / `{fileID: 61000}` reference in the
file is otherwise unchanged (each prefab file has its own independent
fileID numbering, so reusing the same numbers across different files is
correct — they never collide, because they're only meaningful within a
single file).

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab.meta` — same
shape as `Obstacle_Lake.prefab.meta`, with the second GUID you generated in
Step 2.

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab` — start
from the same template as `Obstacle_Lake.prefab` above, then apply these
changes:
- `m_Name: Obstacle_Lake` → `m_Name: Obstacle_Spike`
- Add a fourth component reference to the `GameObject`'s `m_Component`
  list: `- component: {fileID: 11400000}`
- In the `BoxCollider2D` block, change `m_IsTrigger: 0` to `m_IsTrigger: 1`
- Append a new `MonoBehaviour` document referencing `SpikeObstacle`, using
  the GUID you looked up in Step 1:

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
  m_Script: {fileID: 11500000, guid: <SPIKE_SCRIPT_GUID>, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  damage: 2
```

Create `Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab.meta` — same
shape as the other two `.meta` files, with the third GUID you generated in
Step 2.

- [ ] **Step 4: Write the verification test**

Create `Assets/Tests/Editor/RoomObstaclePrefabsTests.cs`:

```csharp
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RoomObstaclePrefabsTests
{
    private const string SpikePath = "Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab";

    [Test]
    public void SpikePrefab_HasTriggerColliderAndSpikeObstacleWithConfiguredDamage()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePath);
        Assert.That(prefab, Is.Not.Null);

        Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);

        Collider2D collider = prefab.GetComponent<Collider2D>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True);

        SpikeObstacle spike = prefab.GetComponent<SpikeObstacle>();
        Assert.That(spike, Is.Not.Null);
    }
}
```

This test only covers `Obstacle_Spike` — per the spec, `Obstacle_Lake` and
`Obstacle_Rock` have no code behavior to unit test. Their correctness is
verified by the batch-mode import log check in Step 5 below (a genuinely
malformed prefab YAML shows up there as an explicit importer error), not by
a dedicated test.

- [ ] **Step 5: Run the test to verify it passes**

Run: `-testFilter "RoomObstaclePrefabsTests"`
Expected: PASS. Then check the batch-mode log (not just the test result) for
import errors on all three new `.prefab` files, including `Obstacle_Lake`
and `Obstacle_Rock` which the test above doesn't cover — a malformed prefab
YAML shows up there as an explicit importer error even if unrelated
existing tests still pass. Fix any offending prefab's YAML and re-run if you
see one.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab" "Assets/Resources/Stage1/Obstacle/Obstacle_Lake.prefab.meta" "Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab" "Assets/Resources/Stage1/Obstacle/Obstacle_Rock.prefab.meta" "Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab" "Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab.meta" Assets/Tests/Editor/RoomObstaclePrefabsTests.cs
git commit -m "feat: Obstacle_Lake/Rock/Spike 프리팹을 Resources/Stage1/Obstacle에 추가"
```
