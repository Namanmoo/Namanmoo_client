using System.Collections.Generic;
using UnityEngine;

public static class Stage1MushroomEncounterSetup
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
        Sprite mushroomSprite,
        Material gateOutlineMaterial = null,
        Material gateFillMaterial = null)
    {
        if (player == null)
        {
            throw new System.ArgumentNullException(nameof(player));
        }

        if (mushroomSprite == null)
        {
            throw new System.ArgumentNullException(nameof(mushroomSprite));
        }

        var enemies = new List<EnemyHealth>(SpawnPositions.Length);
        for (int index = 0; index < SpawnPositions.Length; index++)
        {
            enemies.Add(MushroomFactory.Create(
                parent, player, mushroomSprite, SpawnPositions[index], "Mushroom " + (index + 1)));
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
