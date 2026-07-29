# Visible Health Fill Width Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the health bar's red fill geometry visibly match current health.

**Architecture:** `PlayerHealthBarView` remains the health event subscriber and updates both text and gauge. The gauge uses its left-anchored `RectTransform` width instead of relying on sprite-dependent `Image.Type.Filled` rendering.

**Tech Stack:** Unity 6.0, C#, Unity UI, NUnit/Unity Test Framework

## Global Constraints

- Preserve the current HP bar size, colors, border, and top-left placement.
- Preserve `PlayerHealth.HealthChanged` as the single update source.
- No new image assets or dependencies.

---

### Task 1: Add Visible Width Regression

**Files:**
- Modify: `Assets/Tests/Editor/PlayerHealthBarViewTests.cs`

- [ ] Assert `Fill.anchorMax.x` is 1 at full health, 0.75 after five damage, and 0 at zero health.
- [ ] Run the focused test and confirm the 0.75 assertion fails.

### Task 2: Drive Fill Geometry

**Files:**
- Modify: `Assets/Scripts/UI/PlayerHealthBarUIFactory.cs`
- Modify: `Assets/Scripts/UI/PlayerHealthBarView.cs`

- [ ] Configure the fill as a left-anchored simple Image.
- [ ] In `Render`, clamp the health ratio and assign it to `anchorMax.x`.
- [ ] Preserve four-pixel left, top, bottom, and right padding at full health.
- [ ] Run the focused test and confirm all width states pass.

### Task 3: Rebuild and Verify

**Files:**
- Modify generated scene: `Assets/Scenes/Stage1.unity`

- [ ] Rebuild Stage1.
- [ ] Run all EditMode tests.
- [ ] Run all PlayMode tests.
- [ ] Verify zero failures and inspect the saved Fill anchors.
