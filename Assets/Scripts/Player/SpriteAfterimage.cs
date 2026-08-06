using UnityEngine;

/// <summary>
/// 스윙 잔상 한 장 — 무기 실루엣이 테마색으로 남았다가 스르르 사라진다.
/// 색은 스프라이트에 곱하지 않고 통째로 입힌다(실루엣) — 그림 위에 어색하게
/// 덧칠한 느낌이 아니라 이펙트 자체가 그 색이 되게.
/// </summary>
public sealed class SpriteAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float startAlpha;
    private float lifetime;
    private float age;
    private bool finished;

    /// <summary>수명이 다해 파괴 예약까지 끝났는가 — 뒷북 Advance를 막는다.</summary>
    public bool IsFinished => finished;

    public SpriteRenderer Renderer => spriteRenderer;

    public static SpriteAfterimage Spawn(
        Sprite sprite,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Color color,
        float lifetimeSeconds,
        int sortingOrder)
    {
        var ghostObject = new GameObject("Weapon Afterimage");
        ghostObject.transform.SetPositionAndRotation(position, rotation);
        ghostObject.transform.localScale = scale;

        var ghost = ghostObject.AddComponent<SpriteAfterimage>();
        ghost.spriteRenderer = ghostObject.AddComponent<SpriteRenderer>();
        ghost.spriteRenderer.sprite = sprite;
        ghost.spriteRenderer.color = color;
        ghost.spriteRenderer.sortingOrder = sortingOrder;
        ghost.startAlpha = color.a;
        ghost.lifetime = Mathf.Max(0.01f, lifetimeSeconds);
        return ghost;
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (finished || deltaTime <= 0f)
        {
            return;
        }

        age += deltaTime;
        if (age >= lifetime)
        {
            finished = true;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            return;
        }

        Color color = spriteRenderer.color;
        color.a = AlphaAt(age / lifetime, startAlpha);
        spriteRenderer.color = color;
    }

    /// <summary>진행도(0~1)에 따른 알파 — 시작 알파에서 0으로. EditMode 테스트로 덮는다.</summary>
    public static float AlphaAt(float progress01, float startAlpha)
    {
        return Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(progress01));
    }
}
