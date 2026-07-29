# Stage1 Map 2.5x Scale Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand all playable Stage1 geometry and containment logic to 2.5 times its current footprint without changing the camera view size.

**Architecture:** Preserve the authored outline as source data and derive one cached scaled outline inside `Stage1MapDefinition`. Existing scene and runtime builders keep consuming the public outline, so floor, boundary rendering, collision, and containment stay synchronized.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity Mesh/LineRenderer/EdgeCollider2D, NUnit EditMode and PlayMode tests.

## Global Constraints

- Use an exact map scale of `2.5f`.
- Keep camera orthographic size at `10f`.
- Do not change player position, visual size, collision size, movement speed, item hotbar, boundary line width, or collider edge radius.
- Rebuild `Assets/Scenes/Stage1.unity` after implementation.

---

### Task 1: Scale the Canonical Map Definition

**Files:**
- Modify: `Assets/Scripts/Stage1MapDefinition.cs`
- Modify: `Assets/Tests/Editor/Stage1MapDefinitionTests.cs`

**Interfaces:**
- Produces: `Stage1MapDefinition.Outline` containing cached source coordinates multiplied by `2.5f`.
- Consumes: Existing `OutlinePoints`, triangulation, and containment behavior.

- [ ] **Step 1: Write failing scale and containment tests**

Add literal assertions:

```csharp
Assert.That(Stage1MapDefinition.Outline[0], Is.EqualTo(new Vector2(-22.5f, -20f)));
Assert.That(Stage1MapDefinition.Outline[14], Is.EqualTo(new Vector2(3f, 20f)));
Assert.That(Stage1MapDefinition.Contains(new Vector2(0f, -12.5f)), Is.True);
Assert.That(Stage1MapDefinition.Contains(new Vector2(0f, 22.6f)), Is.False);
```

Replace old unscaled outer-wall test cases with scaled outside points at `x = ±25f` and `y = ±22.6f`.

- [ ] **Step 2: Run `Stage1MapDefinitionTests` and verify RED**

Expected: scale assertions fail because the public outline still returns source coordinates.

- [ ] **Step 3: Implement cached 2.5x coordinates**

Add `MapScale = 2.5f`, create `ScaledOutlinePoints` once by multiplying each source point, triangulate the scaled array, expose it from `Outline`, and make `Contains` read the scaled array.

- [ ] **Step 4: Run `Stage1MapDefinitionTests` and verify GREEN**

Expected: all map-definition tests pass.

### Task 2: Verify Scene Integration and Preserve the Camera

**Files:**
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- Modify: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes: scaled `Stage1MapDefinition.Outline`.
- Produces: Stage1 floor and boundary at 2.5x with camera orthographic size `10f`.

- [ ] **Step 1: Write failing scene integration assertions**

In the playable-stage test, assert:

```csharp
Assert.That(Camera.main.orthographicSize, Is.EqualTo(10f));
Assert.That(edge.points[0], Is.EqualTo(new Vector2(-22.5f, -20f)));
Assert.That(edge.points[edge.pointCount - 1], Is.EqualTo(edge.points[0]));
```

The camera assertion protects the unchanged view; the scaled boundary assertion fails before the new map definition is active.

- [ ] **Step 2: Run `Stage1SceneBuilderTests` and confirm behavior**

Expected after Task 1: scene integration assertions pass because the builder already consumes `Stage1MapDefinition.Outline`; no separate builder scaling is added.

- [ ] **Step 3: Rebuild Stage1**

Run Unity batch mode with `-executeMethod Stage1SceneBuilder.Build`. Expected: exit code 0 and an updated `Assets/Scenes/Stage1.unity`.

- [ ] **Step 4: Inspect the serialized scene**

Verify the camera orthographic size is `10`, the first boundary point is `(-22.5, -20)`, and the player transform remains at `(0, -5, -0.2)` with unit root scale.

### Task 3: Full Regression Verification

**Files:**
- Verify: `Assets/Tests/Editor/*.cs`
- Verify: `Assets/Tests/PlayMode/*.cs`

**Interfaces:**
- Consumes: completed scaled map and rebuilt Stage1 scene.
- Produces: regression evidence for the full project.

- [ ] **Step 1: Run the full EditMode suite**

Expected: zero failures.

- [ ] **Step 2: Run the full PlayMode suite**

Expected: zero failures.

- [ ] **Step 3: Scan logs**

Confirm there are no compiler errors, `NullReferenceException`,
`MissingReferenceException`, or assertion failures.

