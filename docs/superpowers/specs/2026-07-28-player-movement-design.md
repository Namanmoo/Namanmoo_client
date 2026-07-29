# 2D Top-View Player Movement Design

## Goal

Add `Assets/Scripts/PlayerMovement.cs` for a 2D top-view character that moves with WASD using Unity's new Input System.

## Design

- Require a `Rigidbody2D` on the same GameObject with `RequireComponent`.
- Read W, A, S, and D directly through `UnityEngine.InputSystem.Keyboard` during `Update`.
- Normalize diagonal input so diagonal movement is not faster than horizontal or vertical movement.
- Move the body with `Rigidbody2D.MovePosition` during `FixedUpdate` so movement follows the physics timestep.
- Expose movement speed as a positive serialized float with a default value of `5`.
- Stop movement when no movement keys are held.

## Scope

The script supports keyboard WASD movement only. Animation, character rotation, sprinting, rebinding, gamepad input, collision configuration, and scene setup are outside this change.

## Error Handling

If no keyboard is available, the script treats input as zero. `RequireComponent` ensures that Unity adds or requires a `Rigidbody2D`.

## Verification

Add an Edit Mode test for input-to-direction calculation, including diagonal normalization, then compile/run the relevant Unity tests. The movement application remains a thin `Rigidbody2D.MovePosition` call.
