# Title Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Title.png start scene whose game-start button opens Stage1 while settings remains inactive.

**Architecture:** A dedicated scene builder imports the title sprite, creates a Canvas/Image with transparent button overlays, and updates Unity build settings. A small runtime controller owns the Stage1 scene transition.

**Tech Stack:** Unity 6.0, C#, UGUI, Input System, SceneManager, NUnit

## Global Constraints

- Preserve the artwork and text already drawn into `Title.png`.
- Settings has no behavior.
- Title is build index 0 and Stage1 is build index 1.

---

### Task 1: Scene Transition API

**Files:**
- Create: `Assets/Scripts/UI/TitleScreenController.cs`
- Create: `Assets/Tests/Editor/TitleScreenControllerTests.cs`

- [ ] Write a failing test for the Stage1 scene path and start action.
- [ ] Run the focused test and verify failure.
- [ ] Implement the minimal controller using `SceneManager.LoadScene`.
- [ ] Run the focused test and verify success.

### Task 2: Title Scene Builder

**Files:**
- Create: `Assets/Editor/TitleSceneBuilder.cs`
- Create: `Assets/Tests/Editor/TitleSceneBuilderTests.cs`
- Create generated scene: `Assets/Scenes/Title.unity`
- Copy: `Title.png` to `Assets/UI/Title.png`

- [ ] Write failing integration tests for sprite import, Canvas, artwork, transparent buttons, EventSystem, and build order.
- [ ] Run the focused tests and verify failure.
- [ ] Implement the builder and generate Title.unity.
- [ ] Run focused tests and verify success.

### Task 3: Regression Verification

- [ ] Run all EditMode tests.
- [ ] Run all PlayMode tests.
- [ ] Inspect build settings and saved Title scene for required objects.
