using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RoomObstaclePrefabsTests
{
    private const string SpikePath = "Assets/Resources/Stage1/Obstacle/Obstacle_Spike.prefab";

    [Test]
    public void SpikePrefab_HasTriggerColliderAndSpikeObstacleWithConfiguredDamage()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePath);
        Assert.That(prefab, Is.Not.Null);

        Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);

        Collider2D collider = prefab.GetComponent<Collider2D>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True);

        SpikeObstacle spike = prefab.GetComponent<SpikeObstacle>();
        Assert.That(spike, Is.Not.Null);
    }
}
