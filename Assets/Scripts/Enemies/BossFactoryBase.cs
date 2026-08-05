using System;
using UnityEngine;

/// <summary>
/// 여러 보스 팩토리가 공통으로 쓰는 생성 로직(루트 오브젝트, 비주얼 스프라이트,
/// 체력/체력바)을 모은 기반 클래스. 보스별 전투 로직(콜라이더 구성, 컨트롤러
/// 초기화 등)은 각 팩토리가 이 위에 이어서 구현한다.
/// </summary>
public abstract class BossFactoryBase
{
    private const float SpawnZOffset = -0.2f;

    protected static GameObject CreateRoot(Transform worldParent, string name, Vector2 position)
    {
        var root = new GameObject(name);
        root.transform.SetParent(worldParent, false);
        root.transform.position = new Vector3(position.x, position.y, SpawnZOffset);
        return root;
    }

    protected static SpriteRenderer CreateVisual(
        Transform parent, string name, Sprite sprite, float visualHeight, int sortingOrder = 6)
    {
        var visualObject = new GameObject(name);
        visualObject.transform.SetParent(parent, false);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.sortingOrder = sortingOrder;
        float scale = visualHeight / sprite.bounds.size.y;
        visualObject.transform.localScale = Vector3.one * scale;
        return visual;
    }

    protected static EnemyHealth CreateHealth(GameObject boss, int maxHealth, Transform uiParent)
    {
        EnemyHealth health = boss.AddComponent<EnemyHealth>();
        health.Configure(maxHealth);
        if (uiParent != null)
        {
            BossHealthBarUIFactory.Create(uiParent, health);
        }

        return health;
    }

    protected static void RequireNotNull(UnityEngine.Object value, string paramName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
