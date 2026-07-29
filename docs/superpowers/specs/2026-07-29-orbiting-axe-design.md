# Orbiting Axe Design

## Goal

Add `weapon_axe.png` as a default melee weapon in hotbar slot 2. While the axe is selected, pressing or holding any arrow key triggers a player-centered 360-degree axe swing that deals 10 damage and can begin at most once per second.

## Inventory and Input

- Import the source image as `Assets/Weapons/weapon_axe.png`.
- Configure it as a transparent, clamped, non-mipmapped Single Sprite.
- Place one `axe` weapon item in slot 2 while preserving the existing sword in slot 1.
- Show the exact axe Sprite as the slot 2 icon.
- Axe input is active only while slot 2 is selected and the equipped item ID is `axe`.
- Any arrow key starts an attack immediately when the one-second cooldown permits.
- Holding an arrow key repeats attacks once per second.
- Releasing all arrow keys ends the active-input state but does not cancel a swing already in progress.

## Swing Geometry and Timing

- Set the Sprite pivot to the bottom center of the axe handle.
- Position the pivot at the player origin so the handle bottom is the rotation center.
- Scale the visible axe to an appropriate world length while preserving its aspect ratio.
- Begin the swing facing the pressed arrow-key direction.
- Rotate clockwise through exactly 360 degrees over 0.45 seconds.
- Hide and destroy the temporary swing object when the revolution completes.
- Allow a new attack one second after the previous attack began.

## Damage

- Use a trigger collider positioned over the axe blade rather than over the handle.
- Each enemy can receive 10 damage at most once during one swing.
- Multiple different enemies can each receive 10 damage during the same swing.
- Ignore the player, player descendants, and non-enemy colliders.
- Continue using `EnemyHealth.TakeDamage(int)` so existing enemy death behavior remains unchanged.

## Architecture

- `PlayerAxeAttacker` owns input gating, selected-item checks, cooldown timing, and swing creation.
- `AxeSwing` owns rotation progress, per-swing hit deduplication, and trigger damage.
- Stage 1 setup configures the shared inventory with sword in slot 1 and axe in slot 2, then initializes both weapon controllers with the same inventory.
- Both `Stage1SceneBuilder` and `Stage1RuntimeBootstrap` load and validate the axe Sprite.
- The existing sword projectile behavior remains unchanged.

## Validation

- Editor tests cover axe asset import settings, slot 2 inventory configuration, arrow-key direction calculation, cooldown behavior, selected-slot gating, 360-degree rotation, and one-hit-per-enemy behavior.
- Play Mode physics tests verify that a real blade trigger deals exactly 10 damage once per swing.
- Scene-builder tests verify the player, inventory, slot 2 icon, and both editor/runtime construction paths.
- Rebuild Stage 1 and run the complete Edit Mode and Play Mode suites.

## Scope

This iteration does not add knockback, critical hits, attack sounds, particles, directional animation variants, weapon pickups, or runtime weapon-stat upgrades.
