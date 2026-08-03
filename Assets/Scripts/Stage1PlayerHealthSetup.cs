using System;
using UnityEngine;
using UnityEngine.UI;

public static class Stage1PlayerHealthSetup
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public static PlayerHealthBarView Create(
        GameObject player,
        Transform canvasParent,
        Sprite heartSprite)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (heartSprite == null)
        {
            throw new ArgumentNullException(nameof(heartSprite));
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = player.AddComponent<PlayerHealth>();
        }

        if (player.GetComponent<PlayerHealthDebugInput>() == null)
        {
            player.AddComponent<PlayerHealthDebugInput>();
        }

        PlayerDash dash = player.GetComponent<PlayerDash>();
        if (dash == null)
        {
            dash = player.AddComponent<PlayerDash>();
        }

        var canvasObject = new GameObject(
            "Player Health Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(canvasParent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0f;

        PlayerHealthBarView healthView =
            PlayerHealthBarUIFactory.Create(canvasObject.transform, health, heartSprite);
        PlayerDashChargeUIFactory.Create(canvasObject.transform, dash);
        return healthView;
    }
}
