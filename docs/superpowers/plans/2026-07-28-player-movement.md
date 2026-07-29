# 2D Top-View Player Movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a Unity component that moves a 2D top-view character with WASD through the new Input System.

**Architecture:** `PlayerMovement` reads `Keyboard.current` in `Update`, converts the pressed keys into a normalized direction, and applies movement with `Rigidbody2D.MovePosition` in `FixedUpdate`. A small static direction calculation keeps input normalization independently testable.

**Tech Stack:** Unity 6000.5.5f1, C#, Input System 1.19.0, Rigidbody2D, Unity Test Framework 1.7.0

## Global Constraints

- Create `Assets/Scripts/PlayerMovement.cs`.
- Use the new Unity Input System and keyboard WASD input.
- Keep diagonal speed equal to horizontal and vertical speed.
- Move through the 2D physics timestep.
- Do not add animation, rotation, sprinting, rebinding, gamepad support, or scene setup.

---

### Task 1: WASD Direction Calculation and Physics Movement

**Files:**
- Create: `Assets/Tests/Editor/PlayerMovementTests.cs`
- Create: `Assets/Scripts/PlayerMovement.cs`

**Interfaces:**
- Consumes: `UnityEngine.InputSystem.Keyboard`, `UnityEngine.Rigidbody2D`
- Produces: `PlayerMovement.CalculateDirection(Vector2 rawInput)`, returning a direction whose magnitude never exceeds `1`

- [ ] **Step 1: Write the failing direction tests**

```csharp
using NUnit.Framework;
using UnityEngine;

public class PlayerMovementTests
{
    [Test]
    public void CalculateDirection_NormalizesDiagonalInput()
    {
        Vector2 direction = PlayerMovement.CalculateDirection(new Vector2(1f, 1f));

        Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(direction.x, Is.EqualTo(direction.y).Within(0.0001f));
    }

    [Test]
    public void CalculateDirection_PreservesCardinalInput()
    {
        Assert.That(
            PlayerMovement.CalculateDirection(Vector2.left),
            Is.EqualTo(Vector2.left));
    }

    [Test]
    public void CalculateDirection_PreservesZeroInput()
    {
        Assert.That(
            PlayerMovement.CalculateDirection(Vector2.zero),
            Is.EqualTo(Vector2.zero));
    }
}
```

- [ ] **Step 2: Run the Edit Mode tests and verify RED**

Run Unity in batch mode with `-runTests -testPlatform EditMode -testFilter PlayerMovementTests`.

Expected: compilation/test failure because `PlayerMovement` does not exist.

- [ ] **Step 3: Implement the minimal movement component**

Create `PlayerMovement` with `[RequireComponent(typeof(Rigidbody2D))]`, a positive serialized `moveSpeed` defaulting to `5f`, cached `Rigidbody2D`, null-safe keyboard reading in `Update`, and `MovePosition` in `FixedUpdate`. Implement `CalculateDirection` with `Vector2.ClampMagnitude(rawInput, 1f)`.

- [ ] **Step 4: Run the Edit Mode tests and verify GREEN**

Run the same Unity Edit Mode command.

Expected: all `PlayerMovementTests` pass with no compilation errors.

- [ ] **Step 5: Run a Unity batch-mode project compile**

Open the project in Unity batch mode with `-batchmode -nographics -quit`.

Expected: process exits successfully with no C# compiler errors.

- [ ] **Step 6: Commit**

If the project is placed in a Git repository, add the test and script and commit them as `feat: add 2D WASD player movement`. The current workspace is not a Git repository, so no commit is performed here.
