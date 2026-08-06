using UnityEngine;

/// <summary>
/// 날아가는 스프라이트 뒤에 잔상을 흘린다 — 혜성 꼬리.
/// 참격(검기)이 쓰지만 SpriteRenderer가 있는 발사체 무엇에든 붙는다.
/// 잔상 한 장 한 장은 <see cref="SpriteAfterimage"/>가 옅어지며 지운다.
/// </summary>
public sealed class ProjectileTrail : MonoBehaviour
{
    /// <summary>잔상을 남기는 간격 (초).</summary>
    public const float Interval = 0.05f;

    /// <summary>잔상이 사라지기까지 걸리는 시간.</summary>
    public const float GhostLifetime = 0.22f;

    /// <summary>잔상 시작 알파 배율 — 본체보다 옅어야 꼬리로 읽힌다.</summary>
    public const float GhostAlpha = 0.4f;

    private SpriteRenderer spriteRenderer;
    private float nextGhostAt;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Advance(Time.time);
    }

    /// <summary>테스트가 시간을 직접 굴릴 수 있게 열어 둔다.</summary>
    public void Advance(float currentTime)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null
            || currentTime < nextGhostAt)
        {
            return;
        }

        nextGhostAt = currentTime + Interval;

        Color color = spriteRenderer.color;
        color.a *= GhostAlpha;

        SpriteAfterimage.Spawn(
            spriteRenderer.sprite,
            transform.position,
            transform.rotation,
            transform.lossyScale,
            color,
            GhostLifetime,
            spriteRenderer.sortingOrder - 1); // 본체 뒤에 깔린다
    }
}
