using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DungeonEncounterTests
{
    [Test]
    public void SelectDefinitions_ReturnsDeterministicMixedSelection()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { mushroom, squirrel }, count: 6, seed: 1234);

            Assert.That(selected, Has.Length.EqualTo(6));
            Assert.That(selected, Does.Contain(mushroom));
            Assert.That(selected, Does.Contain(squirrel));
            Assert.That(
                DungeonEncounter.SelectDefinitions(
                    new[] { mushroom, squirrel }, 6, 1234),
                Is.EqualTo(selected));
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void SelectDefinitions_CountEqualsDefinitionsReturnsOneOfEach()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { mushroom, squirrel }, 2, 99);

            Assert.That(selected, Has.Length.EqualTo(2));
            Assert.That(selected, Does.Contain(mushroom));
            Assert.That(selected, Does.Contain(squirrel));
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void SelectDefinitions_FiltersNullDefinitions()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new EnemyDefinition[] { null, mushroom, null }, 4, 99);

            Assert.That(selected, Has.Length.EqualTo(4));
            Assert.That(selected, Is.All.SameAs(mushroom));
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
        }
    }

    [Test]
    public void SelectDefinitions_OneValidDefinitionFillsEverySlot()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { mushroom }, 6, 1234);

            Assert.That(selected, Has.Length.EqualTo(6));
            Assert.That(selected, Is.All.SameAs(mushroom));
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
        }
    }

    [Test]
    public void SelectDefinitions_ZeroCountOrNoValidDefinitionsReturnsEmpty()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            Assert.That(DungeonEncounter.SelectDefinitions(new[] { mushroom }, 0, 1234), Is.Empty);
            Assert.That(
                DungeonEncounter.SelectDefinitions(
                    new EnemyDefinition[] { null }, 6, 1234),
                Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
        }
    }

    [Test]
    public void SelectDefinitions_RemainingSlotsUseBothDefinitionsAcrossFixedSeeds()
    {
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();
        bool extraMushroom = false;
        bool extraSquirrel = false;

        try
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                    new[] { mushroom, squirrel }, 3, seed);
                int mushroomCount = System.Array.FindAll(
                    selected, definition => definition == mushroom).Length;
                int squirrelCount = System.Array.FindAll(
                    selected, definition => definition == squirrel).Length;

                Assert.That(mushroomCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(squirrelCount, Is.GreaterThanOrEqualTo(1));
                extraMushroom |= mushroomCount == 2;
                extraSquirrel |= squirrelCount == 2;
            }

            Assert.That(extraMushroom, Is.True);
            Assert.That(extraSquirrel, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(mushroom);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void SelectTemplate_PicksDeterministicallyFromSeed()
    {
        var templateAObject = new GameObject("TemplateA");
        var templateBObject = new GameObject("TemplateB");
        RoomContentTemplate templateA = templateAObject.AddComponent<RoomContentTemplate>();
        RoomContentTemplate templateB = templateBObject.AddComponent<RoomContentTemplate>();

        try
        {
            RoomContentTemplate[] pool = { templateA, templateB };

            RoomContentTemplate first = DungeonEncounter.SelectTemplate(pool, 42);
            RoomContentTemplate second = DungeonEncounter.SelectTemplate(pool, 42);

            Assert.That(first, Is.Not.Null);
            Assert.That(first, Is.SameAs(second));
            Assert.That(pool, Does.Contain(first));
        }
        finally
        {
            Object.DestroyImmediate(templateAObject);
            Object.DestroyImmediate(templateBObject);
        }
    }

    [Test]
    public void SelectTemplate_EmptyOrNullPoolReturnsNull()
    {
        Assert.That(DungeonEncounter.SelectTemplate(null, 1), Is.Null);
        Assert.That(
            DungeonEncounter.SelectTemplate(new RoomContentTemplate[0], 1),
            Is.Null);
        Assert.That(
            DungeonEncounter.SelectTemplate(new RoomContentTemplate[] { null }, 1),
            Is.Null);
    }

    [Test]
    public void Spawn_NormalRoomUsesConfiguredTemplateMarkerPositions()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            mushroom.Configure(
                "dungeon-mushroom",
                "Dungeon Mushroom",
                sprite,
                null,
                EnemyBehaviorType.ChaseContact,
                5,
                3.5f,
                6,
                0.75f,
                1f,
                0f,
                0.01f,
                0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var markerA = new GameObject("MarkerA");
            markerA.transform.SetParent(templateObject.transform, false);
            markerA.transform.localPosition = new Vector3(3f, 4f, 0f);
            markerA.AddComponent<EnemySpawnMarker>();
            var markerB = new GameObject("MarkerB");
            markerB.transform.SetParent(templateObject.transform, false);
            markerB.transform.localPosition = new Vector3(-3f, -2f, 0f);
            markerB.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(mushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(2));

            var positions = new Vector2[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                positions[i] = enemies[i].transform.position;
            }

            Assert.That(positions, Does.Contain(new Vector2(3f, 4f)));
            Assert.That(positions, Does.Contain(new Vector2(-3f, -2f)));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(mushroom);
            Object.DestroyImmediate(sprite);
        }
    }

    [Test]
    public void Spawn_NormalRoomWithNoConfiguredTemplateReturnsNull()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(mushroom, null);

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Null);
            Assert.That(roomRootObject.GetComponentsInChildren<EnemyHealth>(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(mushroom);
        }
    }

    [Test]
    public void Spawn_NonNormalRoomWithConfiguredTemplateReturnsNull()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        EnemyDefinition mushroom = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var marker = new GameObject("MarkerA");
            marker.transform.SetParent(templateObject.transform, false);
            marker.transform.localPosition = new Vector3(3f, 4f, 0f);
            marker.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(mushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Treasure, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Null);
            Assert.That(roomRootObject.GetComponentsInChildren<EnemyHealth>(), Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(mushroom);
        }
    }

    [Test]
    public void Spawn_MarkerWithFixedDefinitionAlwaysSpawnsThatDefinition()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite pooledSprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition pooledMushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            pooledMushroom.Configure(
                "dungeon-mushroom", "Dungeon Mushroom", pooledSprite, null,
                EnemyBehaviorType.ChaseContact, 5, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var markerObject = new GameObject("FixedMarker");
            markerObject.transform.SetParent(templateObject.transform, false);
            markerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker marker = markerObject.AddComponent<EnemySpawnMarker>();
            marker.Configure(fixedEnemy);

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(pooledMushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(1));
            Assert.That(enemies[0].MaxHealth, Is.EqualTo(9));
            Assert.That(enemies[0].gameObject.name, Does.StartWith("Fixed Enemy"));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(pooledMushroom);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(pooledSprite);
            Object.DestroyImmediate(fixedSprite);
        }
    }

    [Test]
    public void Spawn_MixedFixedAndUnfixedMarkersEachResolveCorrectly()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite pooledSprite = Sprite.Create(
            Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition pooledMushroom = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            pooledMushroom.Configure(
                "dungeon-mushroom", "Dungeon Mushroom", pooledSprite, null,
                EnemyBehaviorType.ChaseContact, 5, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var fixedMarkerObject = new GameObject("FixedMarker");
            fixedMarkerObject.transform.SetParent(templateObject.transform, false);
            fixedMarkerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker fixedMarker = fixedMarkerObject.AddComponent<EnemySpawnMarker>();
            fixedMarker.Configure(fixedEnemy);

            var randomMarkerObject = new GameObject("RandomMarker");
            randomMarkerObject.transform.SetParent(templateObject.transform, false);
            randomMarkerObject.transform.localPosition = new Vector3(-5f, 0f, 0f);
            randomMarkerObject.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(pooledMushroom, null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(2));
            int fixedCount = 0;
            int pooledCount = 0;
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy.MaxHealth == 9)
                {
                    fixedCount++;
                }
                else if (enemy.MaxHealth == 5)
                {
                    pooledCount++;
                }
            }

            Assert.That(fixedCount, Is.EqualTo(1));
            Assert.That(pooledCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(pooledMushroom);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(pooledSprite);
            Object.DestroyImmediate(fixedSprite);
        }
    }

    [Test]
    public void Spawn_UnfixedMarkerSkippedWhenPoolEmptyButFixedMarkerStillSpawns()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite fixedSprite = Sprite.Create(
            Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        EnemyDefinition fixedEnemy = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            fixedEnemy.Configure(
                "dungeon-fixed", "Fixed Enemy", fixedSprite, null,
                EnemyBehaviorType.ChaseContact, 9, 3.5f, 6, 0.75f, 1f, 0f, 0.01f, 0.01f);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            var fixedMarkerObject = new GameObject("FixedMarker");
            fixedMarkerObject.transform.SetParent(templateObject.transform, false);
            fixedMarkerObject.transform.localPosition = new Vector3(5f, 0f, 0f);
            EnemySpawnMarker fixedMarker = fixedMarkerObject.AddComponent<EnemySpawnMarker>();
            fixedMarker.Configure(fixedEnemy);

            var randomMarkerObject = new GameObject("RandomMarker");
            randomMarkerObject.transform.SetParent(templateObject.transform, false);
            randomMarkerObject.transform.localPosition = new Vector3(-5f, 0f, 0f);
            randomMarkerObject.AddComponent<EnemySpawnMarker>();

            DungeonEncounter encounter = encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(new EnemyDefinition[0], null);
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(101, Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero, RoomKind.Normal, Doors.North | Doors.South, 2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform, playerObject.transform, shape, room, 202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies = roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(enemies, Has.Length.EqualTo(1));
            Assert.That(enemies[0].MaxHealth, Is.EqualTo(9));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(fixedEnemy);
            Object.DestroyImmediate(fixedSprite);
        }
    }

    [Test]
    public void Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
        var templateObject = new GameObject("Template");
        Sprite contactSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        Sprite rangedSprite = Sprite.Create(
            Texture2D.blackTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        Sprite projectileSprite = Sprite.Create(
            Texture2D.grayTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        EnemyDefinition contact = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition ranged = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            contact.Configure(
                "dungeon-mushroom",
                "Dungeon Mushroom",
                contactSprite,
                null,
                EnemyBehaviorType.ChaseContact,
                5,
                3.5f,
                6,
                0.75f,
                1f,
                0f,
                0.01f,
                0.01f);
            ranged.Configure(
                "dungeon-squirrel",
                "Dungeon Squirrel",
                rangedSprite,
                projectileSprite,
                EnemyBehaviorType.ApproachAndShoot,
                5,
                3.5f,
                6,
                4f,
                1f,
                7f,
                1f,
                0.25f);

            DungeonEncounter encounter =
                encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(new[] { contact, ranged }, null);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            for (int i = 0; i < 4; i++)
            {
                var marker = new GameObject($"Marker{i}");
                marker.transform.SetParent(templateObject.transform, false);
                marker.transform.localPosition = new Vector3(i, 0f, 0f);
                marker.AddComponent<EnemySpawnMarker>();
            }
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(
                101,
                Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero,
                RoomKind.Normal,
                Doors.North | Doors.South,
                2);

            Stage1EncounterGate gate = encounter.Spawn(
                roomRootObject.transform,
                playerObject.transform,
                shape,
                room,
                202);

            Assert.That(gate, Is.Not.Null);
            EnemyHealth[] enemies =
                roomRootObject.GetComponentsInChildren<EnemyHealth>();
            Assert.That(
                roomRootObject.GetComponentsInChildren<ChaseContactEnemyController>(),
                Has.Length.GreaterThanOrEqualTo(1));
            Assert.That(
                roomRootObject.GetComponentsInChildren<ApproachAndShootEnemyController>(),
                Has.Length.GreaterThanOrEqualTo(1));
            Assert.That(enemies, Is.All.Matches<EnemyHealth>(enemy => enemy.MaxHealth == 5));
            Assert.That(
                roomRootObject.GetComponentsInChildren<ChaseContactEnemyController>()[0]
                    .gameObject.name,
                Does.StartWith(contact.DisplayName));
            Assert.That(
                roomRootObject.GetComponentsInChildren<ApproachAndShootEnemyController>()[0]
                    .gameObject.name,
                Does.StartWith(ranged.DisplayName));
        }
        finally
        {
            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(contact);
            Object.DestroyImmediate(ranged);
            Object.DestroyImmediate(contactSprite);
            Object.DestroyImmediate(rangedSprite);
            Object.DestroyImmediate(projectileSprite);
        }
    }

    [Test]
    public void Spawn_SquirrelAttackInitializesProjectileFromDungeonSquirrelDefinition()
    {
        var encounterObject = new GameObject(nameof(DungeonEncounter));
        var roomRootObject = new GameObject(nameof(DungeonRoom));
        var playerObject = new GameObject(nameof(PlayerHealth));
        var templateObject = new GameObject("Template");
        EnemyDefinition mushroom = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
            DungeonEnemyAssetBuilder.MushroomDefinitionPath);
        EnemyDefinition squirrel = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
            DungeonEnemyAssetBuilder.SquirrelDefinitionPath);

        try
        {
            Assert.That(mushroom, Is.Not.Null);
            Assert.That(squirrel, Is.Not.Null);

            DungeonEncounter encounter =
                encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(new[] { mushroom, squirrel }, null);

            RoomContentTemplate template = templateObject.AddComponent<RoomContentTemplate>();
            for (int i = 0; i < 4; i++)
            {
                var marker = new GameObject($"Marker{i}");
                marker.transform.SetParent(templateObject.transform, false);
                marker.transform.localPosition = new Vector3(i, 0f, 0f);
                marker.AddComponent<EnemySpawnMarker>();
            }
            encounter.ConfigureRoomTemplates(new[] { template });

            RoomShape shape = RoomShape.Build(
                101,
                Doors.North | Doors.South);
            var room = new DungeonRoom(
                Vector2Int.zero,
                RoomKind.Normal,
                Doors.North | Doors.South,
                2);

            encounter.Spawn(
                roomRootObject.transform,
                playerObject.transform,
                shape,
                room,
                202);

            ApproachAndShootEnemyController[] controllers =
                roomRootObject.GetComponentsInChildren<ApproachAndShootEnemyController>();
            Assert.That(controllers, Has.Length.GreaterThanOrEqualTo(1));

            ApproachAndShootEnemyController controller = controllers[0];
            playerObject.transform.position = controller.transform.position + Vector3.right;

            Assert.That(controller.TryAttack(0f), Is.True);

            EnemyProjectile[] projectiles =
                Object.FindObjectsByType<EnemyProjectile>();
            Assert.That(projectiles, Has.Length.EqualTo(1));

            EnemyProjectile projectile = projectiles[0];
            Assert.That(projectile.GetComponent<SpriteRenderer>().sprite,
                Is.EqualTo(squirrel.ProjectileSprite));
            Assert.That(projectile.Damage, Is.EqualTo(1));
            Assert.That(projectile.Speed, Is.EqualTo(6f));
            Assert.That(projectile.RemainingLifetime, Is.EqualTo(3f));
            Assert.That(projectile.GetComponent<CircleCollider2D>().radius,
                Is.EqualTo(0.2f));
        }
        finally
        {
            foreach (EnemyProjectile projectile in
                Object.FindObjectsByType<EnemyProjectile>())
            {
                Object.DestroyImmediate(projectile.gameObject);
            }

            Object.DestroyImmediate(encounterObject);
            Object.DestroyImmediate(roomRootObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(templateObject);
        }
    }
}
