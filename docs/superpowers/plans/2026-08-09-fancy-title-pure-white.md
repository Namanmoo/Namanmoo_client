# Fancy Title Pure White Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a non-destructive 1920 x 1080 title-screen variant whose sketchbook paper is neutral pure white while every intentional crayon color and exact title label remains unchanged.

**Architecture:** Edit the text-free scene with the built-in image-generation editor to establish the neutral-white color target. Because the generated edit redraws small details, apply a selective low-chroma/high-luminance correction directly to the approved final so every object and exact text pixel remains registered while only intended-white areas lose their yellow cast.

**Tech Stack:** Built-in image generation/editing, PNG, PowerShell, System.Drawing, Krita/Unity-compatible RGBA 8-bit output.

## Global Constraints

- Input scene: `tmp/imagegen/Title_Fancy_Scene_1920x1080.png`.
- Reference final: `Assets/UI/Title_Fancy_01.png`.
- Create only: `Assets/UI/Title_Fancy_02.png` at exactly 1920 x 1080 pixels.
- Target intended-white base color: `#FFFFFF`; allow only extremely faint neutral-gray paper fibers.
- Preserve the sun, stars, straw hat, magic wand, and weapon accents as intentional yellow crayon.
- Preserve all red, blue, green, and brown crayon colors, object shapes, placements, crop, and outlines.
- Preserve exact copy: `MAKE YOUR`, `OWN WEAPON`, `나 그림왕이 될 거야!`, and `Game Start`.
- Do not modify `Assets/UI/Title_Fancy_01.png` or `Assets/UI/Title.png`.
- Do not commit unless the user explicitly asks.

---

### Task 1: Produce the Pure-White Text-Free Scene

**Files:**
- Read: `tmp/imagegen/Title_Fancy_Scene_1920x1080.png`
- Create: `tmp/imagegen/Title_Fancy_Scene_PureWhite_Source.png`
- Create: `tmp/imagegen/Title_Fancy_Scene_PureWhite_1920x1080.png`

**Interfaces:**
- Consumes: the approved text-free scene as an image-edit target.
- Produces: a 1920 x 1080 PNG with unchanged composition and neutral-white paper areas.

- [ ] **Step 1: Record preservation hashes**

Run `Get-FileHash Assets/UI/Title_Fancy_01.png, Assets/UI/Title.png -Algorithm SHA256` and retain both values for final verification.

- [ ] **Step 2: Inspect the edit target**

Load `tmp/imagegen/Title_Fancy_Scene_1920x1080.png` visually before editing, confirming it contains the approved title illustration with no text overlays.

- [ ] **Step 3: Perform the constrained built-in image edit**

Use `precise-object-edit`: remove beige/cream/yellow cast only from paper, empty title area, speech bubble, button base, white clouds, and the character's intended-white face. Require pure neutral white, forbid object/crop/style changes, forbid text, and explicitly preserve intentional yellow crayon.

- [ ] **Step 4: Persist and normalize the edited scene**

Copy the selected built-in result to `tmp/imagegen/Title_Fancy_Scene_PureWhite_Source.png`. If its dimensions differ from 1920 x 1080, resize with high-quality bicubic interpolation and save `tmp/imagegen/Title_Fancy_Scene_PureWhite_1920x1080.png` as RGBA PNG.

- [ ] **Step 5: Verify color and structure**

Visually compare input and output: the composition and all objects must match, intended yellow crayons must remain yellow, and intended-white areas must have no warm cast. Measure a clear paper sample and require a high, near-neutral RGB mean.

### Task 2: Preserve Exact Copy and Deliver the Unity Asset

**Files:**
- Read: `Assets/UI/Title_Fancy_01.png`
- Create: `Assets/UI/Title_Fancy_02.png`

**Interfaces:**
- Consumes: Task 1's measured neutral-white target and the approved final title image.
- Produces: the final Unity-ready title-screen PNG.

- [ ] **Step 1: Build a selective intended-white mask**

Calculate per-pixel luminance and chroma. Give strong weight only to bright, low-chroma pixels; saturated red, blue, green, orange, pink, yellow, and brown crayon pixels must receive zero or negligible weight.

- [ ] **Step 2: Correct intended-white pixels toward pure white**

Blend only masked pixels toward neutral `#FFFFFF`, preserving faint neutral paper fibers and alpha. Run the correction directly on `Title_Fancy_01.png` so `MAKE YOUR`, `OWN WEAPON`, `나 그림왕이 될 거야!`, and `Game Start` remain geometrically exact.

- [ ] **Step 3: Preserve colored copy and accents**

Visually verify the title colors, blue `Game Start`, red arrow, and yellow outlined stars retain their approved hue, placement, and crayon texture.

- [ ] **Step 4: Save the delivery PNG**

Save `Assets/UI/Title_Fancy_02.png` as 1920 x 1080, RGBA 8-bit PNG. Do not change any Unity scene or prefab reference.

- [ ] **Step 5: Run final verification**

Confirm the final dimensions and pixel format, inspect it visually, compare the preservation hashes, and verify only the new sibling asset and documentation were added. Do not commit.
