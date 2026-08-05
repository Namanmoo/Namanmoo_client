using UnityEngine;

public sealed class SlimeFallMaker : MonoBehaviour
{
    private Transform target;
    private float speed;

    public void Initialize(Transform newTarget, float newSpeed)
    {
        target = newTarget;
        speed = Mathf.Max(0f, newSpeed);
    }

    private void Update() => Advance(Time.deltaTime);

    public void Advance(float deltaTime)
    {
        if (target == null) return;
        transform.position = Vector2.MoveTowards(
            transform.position, target.position, speed * Mathf.Max(0f, deltaTime));
    }
}
