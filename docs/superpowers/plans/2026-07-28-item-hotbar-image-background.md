# Item Hotbar Image Background Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generated hotbar artwork with the complete edited `ItemUI.png` image while keeping one dynamic blue selection outline, aligned item icons, and all existing equipment behavior.

**Architecture:** An edited full-image Sprite becomes the only static visual layer. `ItemHotbarUIFactory` places six transparent normalized overlay regions above it; `ItemHotbarView` continues to own icon and selection-state refresh. Stage 1 keeps using the existing shared setup and persists the imported Sprite reference.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity uGUI 2.5.0, ImageGen image editing, Unity Test Framework 1.7.0.

## Global Constraints

- Preserve the complete source image dimensions, aspect ratio, white background, whitespace, black hand-drawn lines, and labels.
- Remove only the blue outline baked into slot 1.
- Keep the original root-level `ItemUI.png` untouched.
- Store the edited Sprite at `Assets/UI/ItemUIBackground.png`.
- Render no generated black borders, dividers, number labels, decorative backgrounds, gradients, or shadows.
- Keep exactly one dynamic blue outline and six icon overlays.
- Preserve all acquisition, equipment, keyboard, reload, and EventSystem behavior.

---

### Task 1: Edited Background Asset

**Files:**
- Source: `ItemUI.png`
- Create: `Assets/UI/ItemUIBackground.png`
- Create through Unity import: `Assets/UI/ItemUIBackground.png.meta`

**Interfaces:**
- Produces a full-resolution image with the original artwork unchanged except for removal of slot 1's blue outline.
- Produces one Unity Sprite asset loadable at `Assets/UI/ItemUIBackground.png`.

- [ ] **Step 1: Inspect the original at full resolution**

Record its pixel dimensions and identify the blue pixels around slot 1 without changing the source file.

- [ ] **Step 2: Edit a copied image using the image-editing skill**

Remove the blue outline by reconstructing the underlying white paper area while retaining adjacent black slot lines. Preserve the canvas size and all other pixels/artwork.

- [ ] **Step 3: Visually verify the edited asset**

Compare the source and edited images. Confirm the blue mark is gone, the slot 1 black boundary remains, and the white background, whitespace, labels, remaining boxes, and source dimensions match.

- [ ] **Step 4: Import as a single Sprite**

Place the verified output at `Assets/UI/ItemUIBackground.png`. Configure it as `Sprite (2D and UI)`, single mode, alpha enabled, wrap mode clamp, and filtering appropriate for the scanned artwork.

### Task 2: Image-Backed Overlay Layout

**Files:**
- Modify: `Assets/Scripts/Items/ItemHotbarView.cs`
- Modify: `Assets/Scripts/Items/ItemHotbarUIFactory.cs`
- Modify: `Assets/Tests/Editor/ItemHotbarViewTests.cs`

**Interfaces:**
- Consumes the imported background Sprite.
- Produces stable objects `Item Hotbar`, `Background`, `Slot 1` through `Slot 6`, `Icon`, and `Selection Outline`.
- Produces centralized normalized slot rectangles measured from the complete source image.

- [ ] **Step 1: Write failing structure/layout tests**

Assert one `Background` Image exists and uses `ItemUIBackground`; no `Border`, `Divider`, or `Number` objects exist; the background preserves the source aspect ratio; six slots use the exact centralized normalized rectangles; and only one selection outline is active.

- [ ] **Step 2: Run focused Edit Mode tests and confirm RED**

Run Unity Edit Mode tests filtered to `ItemHotbarViewTests`.

Expected: the existing factory still creates generated borders/numbers and has no background Sprite.

- [ ] **Step 3: Implement the image-backed factory**

Create a full-size background Image with `preserveAspect = true`. Remove generated border/divider/number construction. Define six normalized Rect values from measured source-image coordinates and create transparent icon/selection overlay roots using those anchors.

- [ ] **Step 4: Preserve dynamic state rendering**

Keep the existing serialized controller/icon/selection arrays, reconnect lifecycle, icon aspect preservation, and exactly-one-outline refresh behavior.

- [ ] **Step 5: Run focused tests and confirm GREEN**

Run `ItemHotbarViewTests` and confirm every image, coordinate, icon, and selection assertion passes.

### Task 3: Stage 1 Asset Wiring and Runtime Verification

**Files:**
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1ItemHotbarSetup.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`
- Modify: `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`
- Regenerate: `Assets/Scenes/Stage1.unity`

**Interfaces:**
- Consumes `Assets/UI/ItemUIBackground.png`.
- Passes the imported Sprite into the shared hotbar factory for editor-built and runtime-created stages.

- [ ] **Step 1: Write failing Stage 1 and Play Mode assertions**

Assert the saved/runtime hotbar contains the imported background Sprite, lacks generated static artwork, displays acquired icons over the correct normalized slot, and moves one blue outline when selection changes.

- [ ] **Step 2: Run focused tests and confirm RED**

Run focused Stage 1 Edit Mode and hotbar Play Mode tests.

Expected: shared setup does not yet provide the imported Sprite.

- [ ] **Step 3: Wire the imported Sprite**

Load the Sprite through `AssetDatabase` in the editor builder and serialize it into the generated hierarchy. Provide a runtime-safe serialized/default reference path through shared setup without loading from the root filesystem.

- [ ] **Step 4: Rebuild Stage 1**

Run `Stage1SceneBuilder.Build` in Unity batch mode and confirm the saved scene contains one background Sprite plus six overlays.

- [ ] **Step 5: Run full verification**

Run all Edit Mode and Play Mode tests, inspect logs for compile/runtime errors, inspect the saved scene, and visually compare the rendered hotbar with the edited source image.

Expected: all tests pass; the entire image is visible; no baked blue mark remains; exactly one dynamic blue outline moves across the six drawn slots.
