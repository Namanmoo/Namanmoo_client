using System;
using UnityEngine;

public static class PlayerDashChargeUIFactory
{
    public static readonly Vector2 TopLeftInset = new Vector2(24f, -82f);

    public static PlayerDashChargeView Create(Transform parent, PlayerDash dash)
    {
        if (dash == null)
        {
            throw new ArgumentNullException(nameof(dash));
        }

        var root = new GameObject(
            nameof(PlayerDashChargeView),
            typeof(RectTransform),
            typeof(PlayerDashChargeView));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = TopLeftInset;

        PlayerDashChargeView view = root.GetComponent<PlayerDashChargeView>();
        view.Initialize(dash);
        return view;
    }
}
