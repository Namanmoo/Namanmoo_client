# Stage1 Robot Boss Design

## Encounter Flow

The five lower-room krabs continue controlling the middle gate. After all five
die, the gate opens. When the player crosses the upper-room entry trigger, the
encounter spawns one robot boss at the upper-room center and closes the same
gate. The gate remains closed during combat and reopens when the boss dies.

## Boss

- Sprite: `Assets/boss_robot.png`
- Spawn position: upper-room center `(0, 13)`
- Maximum/current health: 100
- Chase speed: 1.25 units per second, 25% of player speed
- Contact damage: 4
- Player invulnerability after boss or bullet damage: one second
- Boss and player overlap without collision response; the boss still respects
  the map boundary.

## Attack State Machine

- The boss chooses a pattern randomly after every three-second chase interval.
- Above 50% HP, bullet and dash patterns each have 50% probability.
- At or below 50% HP, the sprite becomes lightly red and dash probability
  becomes 70%.

### Bullet Pattern

- Fire three radial waves separated by 0.15 seconds.
- Each wave fires one small circular bullet in each of eight cardinal and
  diagonal directions.
- Total per pattern: 24 bullets.
- Bullet speed: 3.75 units per second, 75% of player speed.
- Bullet damage: 4.
- Bullets return to a reusable pool on player contact or after six seconds.

### Dash Pattern

- Stop for two seconds.
- Lock the current direction to the player.
- Dash for 0.6 seconds at 10 units per second, twice player speed.
- Stop for two seconds.
- Resume the common three-second chase interval.

## Boss Health UI

- A boss-only health bar appears at the top center when the boss spawns.
- It starts at `100/100`, updates from boss `EnemyHealth`, and hides on death.
- The existing player health bar remains unchanged at the top left.

## Testing

- Gate explicit close/reopen behavior.
- Entry trigger requires the lower encounter gate to be open and starts once.
- Boss health, speed, damage, rage threshold, pattern probability, dash speed,
  and radial directions.
- Exactly three waves and eight bullets per wave.
- Boss HP bar values and visible fill width.
- Saved Stage1 references, upper-room trigger, sprite import, and build/runtime
  setup parity.
- Full EditMode and PlayMode regression suites.
