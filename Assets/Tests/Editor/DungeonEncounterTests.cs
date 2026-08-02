using NaManMoo.Dungeon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DungeonEncounterTests
{
    [Test]
    public void SelectDefinitions_ReturnsDeterministicMixedSelection()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { krab, squirrel }, count: 6, seed: 1234);

            Assert.That(selected, Has.Length.EqualTo(6));
            Assert.That(selected, Does.Contain(krab));
            Assert.That(selected, Does.Contain(squirrel));
            Assert.That(
                DungeonEncounter.SelectDefinitions(
                    new[] { krab, squirrel }, 6, 1234),
                Is.EqualTo(selected));
        }
        finally
        {
            Object.DestroyImmediate(krab);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void SelectDefinitions_CountEqualsDefinitionsReturnsOneOfEach()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { krab, squirrel }, 2, 99);

            Assert.That(selected, Has.Length.EqualTo(2));
            Assert.That(selected, Does.Contain(krab));
            Assert.That(selected, Does.Contain(squirrel));
        }
        finally
        {
            Object.DestroyImmediate(krab);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void SelectDefinitions_FiltersNullDefinitions()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new EnemyDefinition[] { null, krab, null }, 4, 99);

            Assert.That(selected, Has.Length.EqualTo(4));
            Assert.That(selected, Is.All.SameAs(krab));
        }
        finally
        {
            Object.DestroyImmediate(krab);
        }
    }

    [Test]
    public void SelectDefinitions_OneValidDefinitionFillsEverySlot()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                new[] { krab }, 6, 1234);

            Assert.That(selected, Has.Length.EqualTo(6));
            Assert.That(selected, Is.All.SameAs(krab));
        }
        finally
        {
            Object.DestroyImmediate(krab);
        }
    }

    [Test]
    public void SelectDefinitions_ZeroCountOrNoValidDefinitionsReturnsEmpty()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            Assert.That(DungeonEncounter.SelectDefinitions(new[] { krab }, 0, 1234), Is.Empty);
            Assert.That(
                DungeonEncounter.SelectDefinitions(
                    new EnemyDefinition[] { null }, 6, 1234),
                Is.Empty);
        }
        finally
        {
            Object.DestroyImmediate(krab);
        }
    }

    [Test]
    public void SelectDefinitions_RemainingSlotsUseBothDefinitionsAcrossFixedSeeds()
    {
        EnemyDefinition krab = ScriptableObject.CreateInstance<EnemyDefinition>();
        EnemyDefinition squirrel = ScriptableObject.CreateInstance<EnemyDefinition>();
        bool extraKrab = false;
        bool extraSquirrel = false;

        try
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                EnemyDefinition[] selected = DungeonEncounter.SelectDefinitions(
                    new[] { krab, squirrel }, 3, seed);
                int krabCount = System.Array.FindAll(
                    selected, definition => definition == krab).Length;
                int squirrelCount = System.Array.FindAll(
                    selected, definition => definition == squirrel).Length;

                Assert.That(krabCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(squirrelCount, Is.GreaterThanOrEqualTo(1));
                extraKrab |= krabCount == 2;
                extraSquirrel |= squirrelCount == 2;
            }

            Assert.That(extraKrab, Is.True);
            Assert.That(extraSquirrel, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(krab);
            Object.DestroyImmediate(squirrel);
        }
    }

    [Test]
    public void Spawn_NormalRoomBuildsGuaranteedContactAndRangedEnemies()
    {
        var encounterObject = new GameObject("Encounter");
        var roomRootObject = new GameObject("Room");
        var playerObject = new GameObject("Player");
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
                "dungeon-krab",
                "Dungeon Krab",
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
        EnemyDefinition krab = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
            DungeonEnemyAssetBuilder.KrabDefinitionPath);
        EnemyDefinition squirrel = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
            DungeonEnemyAssetBuilder.SquirrelDefinitionPath);

        try
        {
            Assert.That(krab, Is.Not.Null);
            Assert.That(squirrel, Is.Not.Null);

            DungeonEncounter encounter =
                encounterObject.AddComponent<DungeonEncounter>();
            encounter.Configure(new[] { krab, squirrel }, null);

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
        }
    }
}
