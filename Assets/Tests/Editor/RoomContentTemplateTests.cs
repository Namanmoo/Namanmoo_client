using System.Collections.Generic;
using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEngine;

public sealed class RoomContentTemplateTests
{
    [Test]
    public void SpawnMarkers_ReturnsEveryMarkerChildWithWorldPosition()
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
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(2));
            var positions = new List<Vector2>();
            foreach (EnemySpawnMarker marker in markers)
            {
                positions.Add(marker.transform.position);
            }

            Assert.That(positions, Does.Contain(new Vector2(12f, 3f)));
            Assert.That(positions, Does.Contain(new Vector2(6f, 1f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_NoMarkersReturnsEmpty()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();

        try
        {
            Assert.That(template.SpawnMarkers(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_DefaultFixedEnemyDefinitionIsNull()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        markerA.AddComponent<EnemySpawnMarker>();

        try
        {
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(1));
            Assert.That(markers[0].FixedEnemyDefinition, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SpawnMarkers_ReturnsMarkerWithFixedEnemyDefinitionSet()
    {
        var root = new GameObject("Template");
        RoomContentTemplate template = root.AddComponent<RoomContentTemplate>();
        EnemyDefinition fixedDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();

        var markerA = new GameObject("MarkerA");
        markerA.transform.SetParent(root.transform, false);
        EnemySpawnMarker marker = markerA.AddComponent<EnemySpawnMarker>();
        marker.Configure(fixedDefinition);

        try
        {
            List<EnemySpawnMarker> markers = template.SpawnMarkers();

            Assert.That(markers, Has.Count.EqualTo(1));
            Assert.That(markers[0].FixedEnemyDefinition, Is.SameAs(fixedDefinition));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(fixedDefinition);
        }
    }
}
