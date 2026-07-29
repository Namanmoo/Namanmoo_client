using System;
using UnityEngine;
using UnityEngine.UI;

public static class Stage1BossEncounterSetup
{
    public static Stage1BossEncounter Create(
        Transform parent,
        Transform player,
        Stage1EncounterGate gate,
        Sprite bossSprite)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (gate == null)
        {
            throw new ArgumentNullException(nameof(gate));
        }

        if (bossSprite == null)
        {
            throw new ArgumentNullException(nameof(bossSprite));
        }

        var canvasObject = new GameObject(
            "Boss Health Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var triggerObject = new GameObject("Boss Entry Trigger");
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.position = new Vector3(-4.5f, 3.5f, 0f);
        BoxCollider2D trigger = triggerObject.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(13f, 1f);
        trigger.isTrigger = true;
        Stage1BossEncounter encounter =
            triggerObject.AddComponent<Stage1BossEncounter>();
        encounter.Initialize(
            gate,
            player,
            bossSprite,
            parent,
            canvasObject.transform);
        return encounter;
    }
}
