# Task 4 — Stage 1 Integration Report

## Implementation

- Added `Assets/Scripts/Stage1ItemHotbarSetup.cs` as the single shared Stage 1 hotbar setup path.
  - Creates one `PlayerInventory`.
  - Adds and initializes one `ItemHotbarController` on the supplied player with that inventory.
  - Creates `Item Hotbar Canvas` with a `Canvas` in `ScreenSpaceOverlay` mode and a `CanvasScaler` set to `ScaleWithScreenSize` with a `1920 x 1080` reference resolution.
  - Creates the factory-backed `Item Hotbar` under that canvas using the same inventory instance.
  - Does not create a `GraphicRaycaster`, `EventSystem`, pickup, or sample-item object.
- Updated `Assets/Editor/Stage1SceneBuilder.cs` to call the shared setup after the pre-existing player movement/collision setup.
- Updated `Assets/Scripts/Stage1RuntimeBootstrap.cs` to call the same shared setup after the pre-existing player movement/collision setup. The runtime canvas is a child of the existing generated root; the editor-built canvas remains a scene root. In both paths, the canvas-to-hotbar hierarchy itself is identical.
- Extended `Assets/Tests/Editor/Stage1SceneBuilderTests.cs` with an integration test covering the player controller/inventory, canvas mode and scaler, bottom-center hotbar anchoring, six named and labelled slots, initial Slot 1 selection outline, inventory-driven outline movement (which proves the view uses the controller inventory), and absence of the named pickup/sample objects.

## TDD and verification

- The Stage 1 integration test was added before the production integration code.
- Unity red/green execution was not run. At verification time, three `Unity` processes were active (`7928`, `19060`, and `31140`), so launching another Unity process would violate the task instruction not to launch while the project is in use. No `Library/UnityLockfile` was present, but the live editor processes remain the controlling safety signal.
- Static inspection confirms both construction paths call the same `Stage1ItemHotbarSetup.Create` method, so the hotbar setup cannot drift between editor and runtime paths without changing that shared method.
- Git commands and commits were intentionally not used because the parent task specified that Git is invalid.

## Follow-up verification required

After the active editor closes, run the focused EditMode test filter for `Stage1SceneBuilderTests` in Unity and inspect the generated `Stage1` scene/runtime bootstrap visually. This is required before asserting that the integration test compiles and passes in Unity.

## Round 1 test strengthening

- Kept production code unchanged.
- Strengthened `Build_CreatesPlayerOwnedBottomCenterItemHotbar` so that it now asserts exactly six direct `Slot *` children and exactly six `Selection Outline` descendants before it checks initial selection state. This closes the gap where extra slot or outline objects could previously pass the test.
- Added an explicit assertion that `Slot 1/Selection Outline` is active initially, in addition to the single-active-outline count.
- Added an assertion that no `EventSystem` component exists in the built scene, including inactive objects.
- Unity execution remains deferred because the active Unity processes documented above are still present; this test-source change has not been compile- or runtime-verified in Unity.
