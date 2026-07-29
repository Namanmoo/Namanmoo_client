using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerWeaponController : MonoBehaviour
{
    private PlayerInventory inventory;
    private float nextAttackTime;

    private void Update()
    {
        ProcessInput(Keyboard.current, Time.time);
    }

    public void InitializeInventory(PlayerInventory newInventory)
    {
        inventory = newInventory;
    }

    public static Vector2 CalculateCardinalDirection(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        int pressed = 0;
        Vector2 direction = Vector2.zero;
        if (keyboard.upArrowKey.isPressed) { pressed++; direction = Vector2.up; }
        if (keyboard.downArrowKey.isPressed) { pressed++; direction = Vector2.down; }
        if (keyboard.leftArrowKey.isPressed) { pressed++; direction = Vector2.left; }
        if (keyboard.rightArrowKey.isPressed) { pressed++; direction = Vector2.right; }
        return pressed == 1 ? direction : Vector2.zero;
    }

    public void ProcessInput(Keyboard keyboard, float currentTime)
    {
        WeaponDefinition weapon = inventory?.EquippedItem?.Weapon;
        Vector2 direction = CalculateCardinalDirection(keyboard);
        if (weapon == null || !weapon.IsValid || direction == Vector2.zero ||
            currentTime < nextAttackTime)
        {
            return;
        }

        if (weapon.Category == WeaponCategory.Melee)
        {
            ExecuteMelee(weapon, direction);
        }
        else
        {
            SpawnProjectile(weapon, direction);
        }
        nextAttackTime = currentTime + weapon.AttackInterval;
    }

    private void ExecuteMelee(WeaponDefinition weapon, Vector2 direction)
    {
        Collider2D[] candidates = Physics2D.OverlapCircleAll(
            transform.position,
            weapon.Reach + weapon.CollisionRadius);
        var damaged = new HashSet<EnemyHealth>();
        foreach (Collider2D candidate in candidates)
        {
            EnemyHealth health = candidate.GetComponentInParent<EnemyHealth>();
            if (health == null || damaged.Contains(health) ||
                candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!WeaponAttackGeometry.IsMeleeHit(
                    weapon.Type,
                    transform.position,
                    direction,
                    health.transform.position,
                    weapon.Reach,
                    weapon.CollisionRadius,
                    weapon.AttackArc))
            {
                continue;
            }

            damaged.Add(health);
            health.TakeDamage(weapon.Damage);
        }
    }

    private void SpawnProjectile(WeaponDefinition weapon, Vector2 direction)
    {
        var projectileObject = new GameObject(weapon.DisplayName + " Projectile");
        projectileObject.transform.position =
            transform.position + (Vector3)(direction * Mathf.Max(weapon.CollisionRadius, 0.1f));
        projectileObject.AddComponent<WeaponProjectile>()
            .Initialize(weapon, direction, gameObject);
    }
}
