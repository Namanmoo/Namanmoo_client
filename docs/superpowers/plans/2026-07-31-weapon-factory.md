# Weapon Factory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task.

**Goal:** Separate reusable `WeaponDefinition` construction from `SampleWeaponFactory` so backend-facing code can call a public factory.

**Architecture:** `WeaponFactory` owns creation and configuration of runtime `WeaponDefinition` objects. `SampleWeaponFactory` retains only sample loadout values and delegates each definition to the reusable factory.

**Tech Stack:** Unity 6, C#, NUnit Editor tests

## Global Constraints

- Do not change backend, database, scene, dungeon, or SampleStage behavior.
- Keep production changes inside `Assets/Scripts/Items`.
- Do not create a Git commit.

---

### Task 1: Public weapon factory

**Files:**
- Create: `Assets/Scripts/Items/WeaponFactory.cs`
- Create: `Assets/Scripts/Items/WeaponFactory.cs.meta`
- Modify: `Assets/Scripts/Items/SampleWeaponFactory.cs`
- Test: `Assets/Tests/Editor/WeaponFactoryTests.cs`
- Create: `Assets/Tests/Editor/WeaponFactoryTests.cs.meta`

**Interfaces:**
- Produces: `WeaponFactory.CreateWeapon(string, string, WeaponCategory, WeaponType, int, float, float, float, float, float, float, Sprite, Color) : WeaponDefinition`
- Preserves: `SampleWeaponFactory.Create(Sprite, Sprite) : WeaponDefinition[]`

- [ ] **Step 1: Write a failing Editor test**

Create a valid weapon through the wished-for public API and assert every configured property, including using the supplied sprite for both icon and world display.

- [ ] **Step 2: Run the focused test and verify RED**

Run the Unity Editor test assembly filtered to `WeaponFactoryTests`. Expect compilation failure because `WeaponFactory` does not exist.

- [ ] **Step 3: Implement the minimal factory and delegate sample creation**

Move the `ScriptableObject.CreateInstance<WeaponDefinition>()` and `Configure(...)` call into `WeaponFactory.CreateWeapon(...)`. Replace sample factory calls with `WeaponFactory.CreateWeapon(...)`.

- [ ] **Step 4: Run focused and regression tests**

Run `WeaponFactoryTests` and `SampleWeaponFactoryTests`, then the full Editor suite. Expect all tests to pass.
