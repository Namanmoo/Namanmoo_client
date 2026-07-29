using System.Collections.Generic;
using UnityEngine;

public static class Stage1KrabEncounterSetup
{
    private static readonly Vector2[] SpawnPositions =
    {
        new Vector2(-14f, -14f),
        new Vector2(-7f, -15f),
        new Vector2(0f, -14f),
        new Vector2(7f, -15f),
        new Vector2(14f, -14f)
    };

    public static Stage1EncounterGate Create(
        Transform parent,
        Transform player,
        Sprite krabSprite,
        Material gateOutlineMaterial = null,
        Material gateFillMaterial = null)
    {
        if (player == null)
        {
            throw new System.ArgumentNullException(nameof(player));
        }

        if (krabSprite == null)
        {
            throw new System.ArgumentNullException(nameof(krabSprite));
        }

        var enemies = new List<EnemyHealth>(SpawnPositions.Length);
        for (int index = 0; index < SpawnPositions.Length; index++)
        {
            enemies.Add(CreateKrab(parent, player, krabSprite, index));
        }

        var gateObject = new GameObject("Middle Passage Gate");
        gateObject.transform.SetParent(parent, false);
        gateObject.transform.position = new Vector3(-4.5f, 0.5f, -0.1f);

        BoxCollider2D barrier = gateObject.AddComponent<BoxCollider2D>();
        barrier.size = new Vector2(13f, 0.6f);

        LineRenderer outline = CreateGateLine(
            gateObject.transform,
            "Gate Outline",
            0.8f,
            5,
            gateOutlineMaterial,
            new Color(0.22f, 0.06f, 0.04f, 1f));
        LineRenderer fill = CreateGateLine(
            gateObject.transform,
            "Gate Fill",
            0.52f,
            6,
            gateFillMaterial,
            new Color(0.85f, 0.15f, 0.08f, 1f));

        Stage1EncounterGate gate = gateObject.AddComponent<Stage1EncounterGate>();
        gate.Initialize(enemies, barrier, new Renderer[] { outline, fill });
        return gate;
    }

    private static EnemyHealth CreateKrab(
        Transform parent,
        Transform player,
        Sprite sprite,
        int index)
    {
        var krab = new GameObject("Krab " + (index + 1));
        krab.transform.SetParent(parent, false);
        Vector2 position = SpawnPositions[index];
        krab.transform.position = new Vector3(position.x, position.y, -0.2f);

        var visual = new GameObject("Krab Visual");
        visual.transform.SetParent(krab.transform, false);
        float visualScale = 2f / sprite.bounds.size.y;
        visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;

        Rigidbody2D body = krab.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = krab.AddComponent<CircleCollider2D>();
        collider.radius = 0.7f;
        collider.isTrigger = false;

        var sensorObject = new GameObject("Krab Contact Sensor");
        sensorObject.transform.SetParent(krab.transform, false);
        CircleCollider2D sensor = sensorObject.AddComponent<CircleCollider2D>();
        sensor.radius = 0.75f;
        sensor.isTrigger = true;

        EnemyHealth health = krab.AddComponent<EnemyHealth>();
        health.Configure(5);
        KrabEnemy enemy = krab.AddComponent<KrabEnemy>();
        enemy.Initialize(player);
        return health;
    }

    private static LineRenderer CreateGateLine(
        Transform parent,
        string name,
        float width,
        int sortingOrder,
        Material material,
        Color color)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(-6.5f, 0f, 0f));
        line.SetPosition(1, new Vector3(6.5f, 0f, 0f));
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.sortingOrder = sortingOrder;
        line.startColor = color;
        line.endColor = color;
        if (material != null)
        {
            line.sharedMaterial = material;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            line.material = new Material(shader) { color = color };
        }

        return line;
    }
}
