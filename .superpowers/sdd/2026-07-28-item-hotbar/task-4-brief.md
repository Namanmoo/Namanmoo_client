### Task 4: Stage 1 Integration

Modify:
- `Assets/Editor/Stage1SceneBuilder.cs`
- `Assets/Scripts/Stage1RuntimeBootstrap.cs`
- `Assets/Tests/Editor/Stage1SceneBuilderTests.cs`

Consume:
- `PlayerInventory`
- `ItemHotbarController.Initialize(PlayerInventory)`
- `ItemHotbarUIFactory.Create(Transform, PlayerInventory)`

Required behavior:
- Both editor-built Stage 1 and runtime-bootstrap Stage 1 create the same hotbar hierarchy and behavior.
- Add `ItemHotbarController` to the player.
- Construct exactly one `PlayerInventory`, initialize the controller with it, and pass the same instance to the view factory.
- External acquisition systems can reach the inventory through the controller's read-only `Inventory` property.
- Create a GameObject named `Item Hotbar Canvas`.
- Add `Canvas` configured as `ScreenSpaceOverlay`.
- Add `CanvasScaler` with `ScaleWithScreenSize` and a reasonable project reference resolution.
- Add `GraphicRaycaster` only if needed by uGUI; do not add decorative UI.
- Call `ItemHotbarUIFactory.Create` under the canvas.
- The factory-created `Item Hotbar` remains bottom-center anchored.
- Do not add world pickup objects or sample items.
- Do not add an EventSystem because the hotbar is non-interactive and input is read directly by `ItemHotbarController`.

Builder tests:
- Build Stage 1.
- Find the Player and assert it has `ItemHotbarController` with a non-null `Inventory`.
- Find `Item Hotbar Canvas`, assert overlay render mode and scaler settings.
- Find `Item Hotbar` and assert six named slots with exact labels `1..6`.
- Assert exactly one selection outline is active initially and it belongs to `Slot 1`.
- Assert there are no pickup/sample item objects.

Runtime bootstrap tests:
- Add focused coverage if existing tests make it practical; otherwise ensure the shared setup path is small and statically identical.

Follow TDD. The Unity project may still be locked by the active editor; do not launch duplicate Unity processes. Document verification limitations. Preserve existing stage map/player movement behavior.
