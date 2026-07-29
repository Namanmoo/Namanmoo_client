# Task 1: Add Enemy Health

## Requirements

- Create `Assets/Scripts/Combat/EnemyHealth.cs`.
- Create `Assets/Tests/Editor/EnemyHealthTests.cs`.
- Produce `EnemyHealth.CurrentHealth`, `EnemyHealth.MaxHealth`, and `EnemyHealth.TakeDamage(int amount)`.
- Use `[SerializeField, Min(1)] private int maxHealth = 20`.
- Initialize current health from maximum health in `Awake`.
- Ignore zero and negative damage.
- Clamp subtraction and destroy the enemy GameObject when health reaches zero.
- Tests must cover initial 20 health, 5 damage leaving 15, ignored zero/negative damage, and lethal destruction after a frame.
- Follow strict TDD: run RED before production code and GREEN afterward.
- Do not modify unrelated files.

## Report Contract

Write `.superpowers/sdd/2026-07-29-spinning-sword-projectile/task-1-report.md` with:

- Status (`DONE`, `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT`, or `BLOCKED`).
- Files changed.
- RED command and observed failure.
- GREEN command and exact totals.
- Self-review notes and concerns.
