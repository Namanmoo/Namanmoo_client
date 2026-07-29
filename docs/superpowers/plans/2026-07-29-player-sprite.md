# Player Sprite Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generated blue player circle with the supplied `player.png` artwork without changing movement, collision, inventory, or hotbar behavior.

**Architecture:** Import the supplied image as one project Sprite and make both player creation paths require that same asset. A shared height constant determines renderer scale while the existing physics components remain unchanged.

**Tech Stack:** Unity 6000.5.5f1, C#, Unity 2D SpriteRenderer and Physics2D, NUnit EditMode and PlayMode tests.

## Global Constraints

- Preserve the root `player.png`.
- Use `Assets/Player/player.png` as a Single Sprite with transparency.
- Preserve the source 221:354 aspect ratio and render it approximately 2 world units tall.
- Keep `CircleCollider2D.radius` at 0.5 and retain all existing movement and item-hotbar behavior.
- Missing player Sprite references must fail before player creation with an actionable error.

---

### Task 1: Import and Specify the Player Sprite

**Files:**
- Create: `Assets/Player/player.png`
- Create: `Assets/Player/player.png.meta`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

**Interfaces:**
- Consumes: root `player.png`, dimensions 221×354 with alpha.
- Produces: project Sprite at `Assets/Player/player.png`.

- [ ] **Step 1: Copy the source asset**

Copy the root image byte-for-byte to `Assets/Player/player.png`, leaving the source untouched.

- [ ] **Step 2: Import once and write the failing asset assertions**

Add `PlayerSpritePath = "Assets/Player/player.png"` and assertions that the asset loads as a Sprite, has a 221×354 texture and rect, uses `SpriteImportMode.Single`, enables alpha transparency, disables mipmaps, clamps wrapping, and uses at least 512 max texture size.

- [ ] **Step 3: Run the focused test and verify RED**

Run `Stage1SceneBuilderTests` in EditMode. Expected: failure because the scene builder has not applied the Sprite and scale yet.

- [ ] **Step 4: Configure import settings**

Set Texture Type to Sprite, Sprite Mode to Single, Pixels Per Unit to 100, alpha transparency on, mipmaps off, clamp wrapping, max size 512, and uncompressed texture compression.

- [ ] **Step 5: Run the asset assertions**

Run `Stage1SceneBuilderTests` and confirm the import-specific assertions pass while player-rendering assertions remain RED.

### Task 2: Apply the Sprite in Both Player Creation Paths

**Files:**
- Modify: `Assets/Editor/Stage1SceneBuilder.cs`
- Modify: `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- Modify: `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

**Interfaces:**
- Consumes: Sprite at `Assets/Player/player.png`.
- Produces: `Stage1SceneBuilder.PlayerVisualHeight = 2f` and equivalent runtime player rendering.

- [ ] **Step 1: Write failing player-rendering tests**

Assert that the saved player renderer:

```csharp
Assert.That(renderer.sprite, Is.SameAs(expectedPlayerSprite));
Assert.That(renderer.color, Is.EqualTo(Color.white));
Assert.That(renderer.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
Assert.That(
    renderer.bounds.size.x / renderer.bounds.size.y,
    Is.EqualTo(221f / 354f).Within(0.01f));
Assert.That(player.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.5f));
```

Extend runtime validation tests to verify the editor assigns both serialized Sprites and that a missing player Sprite throws an `InvalidOperationException` containing `Assets/Player/player.png`.

- [ ] **Step 2: Run focused EditMode tests and verify RED**

Run the `Stage1SceneBuilderTests` filter. Expected failures: generated circle Sprite, blue tint, and missing runtime field/guard.

- [ ] **Step 3: Implement the scene-builder path**

Replace `PlayerTexturePath` and `GetOrCreatePlayerSprite()` with a required `PlayerSpritePath`. Load the Sprite through `AssetDatabase.LoadAssetAtPath<Sprite>`, throw if absent, assign `Color.white`, and compute uniform scale:

```csharp
float scale = 2f / sprite.bounds.size.y;
playerObject.transform.localScale = new Vector3(scale, scale, 1f);
```

Leave Rigidbody2D, CircleCollider2D, movement, and hotbar setup unchanged.

- [ ] **Step 4: Implement the runtime-bootstrap path**

Add serialized `Sprite playerSprite`, assign it in `Reset`/`OnValidate`, validate it beside the hotbar background, and apply it with the same white color and 2-unit scale. Remove `CreateCircleSprite()`.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run `Stage1SceneBuilderTests`. Expected: all tests pass.

### Task 3: Rebuild the Scene and Run Regression Tests

**Files:**
- Modify: `Assets/Scenes/Stage1.unity`
- Verify: `Assets/Tests/Editor/PlayerMovementTests.cs`
- Verify: `Assets/Tests/PlayMode/ItemHotbarPlayModeTests.cs`

**Interfaces:**
- Consumes: completed asset and player creation changes.
- Produces: saved Stage1 scene referencing the imported player Sprite.

- [ ] **Step 1: Rebuild Stage1**

Run Unity batch mode with `-executeMethod Stage1SceneBuilder.Build`. Expected: exit code 0.

- [ ] **Step 2: Inspect the serialized scene**

Verify the `Player` SpriteRenderer references the GUID for `Assets/Player/player.png`, has white color, correct uniform scale, sorting order 4, and retains a radius-0.5 CircleCollider2D.

- [ ] **Step 3: Run the full EditMode suite**

Run Unity EditMode tests. Expected: zero failures.

- [ ] **Step 4: Run the full PlayMode suite**

Run Unity PlayMode tests. Expected: zero failures.

- [ ] **Step 5: Scan logs**

Confirm there are no compiler errors, `NullReferenceException`, `MissingReferenceException`, or assertion failures.

