using UnityEngine;

/// <summary>
/// 카메라 흔들림 효과. 어디서든 <see cref="Trigger"/>로 호출할 수 있는 범용 컴포넌트라
/// 술탄 보스 착지 외에 다른 보스·타격 이벤트에도 그대로 재사용할 수 있다.
/// </summary>
public sealed class CameraShake : MonoBehaviour
{
    private float intensity;
    private float duration;
    private float remaining;
    private Vector2 currentOffset;

    /// <summary>이번 프레임에 카메라 위치에 더할 오프셋.</summary>
    public Vector2 CurrentOffset => currentOffset;

    public bool IsShaking => remaining > 0f;

    /// <summary>
    /// 흔들림을 건다. 이미 흔들리는 중이면 더 강하고 더 오래 남은 쪽을 유지한다 —
    /// <see cref="EnemyStatus"/>의 경직·둔화처럼 약한 재적용이 강한 효과를 끊지 않게 한다.
    /// </summary>
    public void Trigger(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f)
        {
            return;
        }

        this.intensity = Mathf.Max(this.intensity, intensity);
        this.duration = Mathf.Max(this.duration, duration);
        remaining = Mathf.Max(remaining, duration);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>테스트가 시간을 직접 굴릴 수 있게 열어 둔다.</summary>
    public void Tick(float deltaTime)
    {
        remaining = Mathf.Max(0f, remaining - deltaTime);

        if (remaining <= 0f)
        {
            currentOffset = Vector2.zero;
            return;
        }

        float ratio = remaining / duration;
        currentOffset = Random.insideUnitCircle * intensity * ratio;
    }
}
