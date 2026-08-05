using NUnit.Framework;
using UnityEngine;

public sealed class EnemyProjectileInitialRotationTests
{
    [Test]
    public void Initialize_AppliesConfiguredInitialVisualRotation()
    {
        var projectileObject = new GameObject("Oriented Projectile");
        try
        {
            EnemyProjectile projectile =
                projectileObject.AddComponent<EnemyProjectile>();
            projectile.Initialize(
                null, null, Vector2.down, 1, 0f, 2f, 0.1f, 0f, 90f);

            Assert.That(
                Mathf.DeltaAngle(projectile.transform.eulerAngles.z, 90f),
                Is.Zero.Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(projectileObject);
        }
    }
}
