using System.Collections.Generic;
using UnityEngine;

public sealed class AxeSwing : MonoBehaviour
{
    private readonly HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
    private GameObject owner;
    private int damage;
    private float duration;
    private float elapsedTime;
    private bool initialized;
    private bool completed;

    public void Initialize(GameObject newOwner, int newDamage, float newDuration)
    {
        owner = newOwner;
        damage = Mathf.Max(0, newDamage);
        duration = Mathf.Max(0.0001f, newDuration);
        elapsedTime = 0f;
        completed = false;
        initialized = true;
        damagedEnemies.Clear();
    }

    private void Update()
    {
        if (initialized)
        {
            Advance(Time.deltaTime);
        }
    }

    public void Advance(float deltaTime)
    {
        if (!initialized || completed || deltaTime <= 0f)
        {
            return;
        }

        float remainingTime = duration - elapsedTime;
        float appliedTime = Mathf.Min(deltaTime, remainingTime);
        transform.Rotate(0f, 0f, -360f * (appliedTime / duration));
        elapsedTime += appliedTime;

        if (elapsedTime >= duration)
        {
            completed = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    public bool TryHit(Collider2D other)
    {
        if (completed || other == null || IsOwnerCollider(other))
        {
            return false;
        }

        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || damagedEnemies.Contains(enemyHealth))
        {
            return false;
        }

        damagedEnemies.Add(enemyHealth);
        enemyHealth.TakeDamage(damage);
        return true;
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        return owner != null && other.transform.IsChildOf(owner.transform);
    }
}
