using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomContentTemplateTests
{
    [Test]
    public void SpawnMarkerPositions_ReturnsWorldPositionOfEveryMarkerChild()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        root.transform.position = new Vector3(10f, 0f, 0f);

        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        markerA.transform.localPosition = new Vector3(2f, 3f, 0f);
        markerA.AddComponent<EnemySpawnMarker>();

        var markerB = new GameObject("MarkerB");
        markerB.transform.SetParent(root.transform, false);
        markerB.transform.localPosition = new Vector3(-4f, 1f, 0f);
        markerB.AddComponent<EnemySpawnMarker>();

        var decoration = new GameObject("Obstacle");
        decoration.transform.SetParent(root.transform, false);

        try
        {
            List<Vector2> positions = template.SpawnMarkerPositions();

            Assert.That(positions, Has.Count.EqualTo(2));
            Assert.That(positions, Does.Contain(new Vector2(12f, 3f)));
            Assert.That(positions, Does.Contain(new Vector2(6f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkerPositions_NoMarkersReturnsEmpty()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();

        try
        {
            Assert.That(template.SpawnMarkerPositions(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
