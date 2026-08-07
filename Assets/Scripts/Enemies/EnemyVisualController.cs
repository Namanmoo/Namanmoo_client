using UnityEngine;

public sealed class EnemyVisualController : MonoBehaviour
{
    private const string AttackTrigger = "Attack";

    // 좌우 모션을 가진 적이 쓰는 스테이트 이름. 네 개가 모두 있는 컨트롤러만 방향 전환을 한다.
    private const string IdleLeft = "Idle_Left";
    private const string IdleRight = "Idle_Right";
    private const string MoveLeft = "Move_Left";
    private const string MoveRight = "Move_Right";

    /// <summary>이 속도(초당 유닛)를 넘으면 이동으로 본다. 물리 흔들림에 반응하지 않을 만큼 작다.</summary>
    private const float MoveSpeedThreshold = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool directional;
    private Transform bodyTransform;
    private Vector3 lastPosition;
    private bool facingRight = true;
    private string currentState;

    public void Configure(Sprite sprite, RuntimeAnimatorController animatorController)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;

        if (animatorController == null)
        {
            animator = null;
            return;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = animatorController;

        // 네 스테이트를 다 갖춘 컨트롤러만 방향 전환 대상이다.
        // 기존 적들(단일 스프라이트, 컨트롤러 없음)은 여기서 걸러져 동작이 그대로 유지된다.
        directional = HasState(IdleLeft) && HasState(IdleRight)
            && HasState(MoveLeft) && HasState(MoveRight);

        // 이동량은 루트에서 재야 한다. 이 오브젝트는 자식이라 로컬 위치가 늘 0이다.
        bodyTransform = transform.parent != null ? transform.parent : transform;
        lastPosition = bodyTransform.position;
    }

    private void Update()
    {
        if (!directional || animator == null)
        {
            return;
        }

        Vector3 delta = bodyTransform.position - lastPosition;
        lastPosition = bodyTransform.position;

        // MovePosition으로 움직여서 velocity를 믿을 수 없다. 실제 위치 변화로 판단한다.
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        bool moving = speed > MoveSpeedThreshold;

        // 세로로만 움직이면 x가 0에 가까워 마지막으로 본 방향을 유지한다.
        if (moving && Mathf.Abs(delta.x) > Mathf.Epsilon)
        {
            facingRight = delta.x > 0f;
        }

        Play(moving
            ? (facingRight ? MoveRight : MoveLeft)
            : (facingRight ? IdleRight : IdleLeft));
    }

    private void Play(string state)
    {
        if (state == currentState)
        {
            return;
        }

        currentState = state;
        animator.Play(state, 0, 0f);
    }

    private bool HasState(string state)
    {
        return animator.HasState(0, Animator.StringToHash(state));
    }

    public void PlayAttack()
    {
        if (animator != null && HasAttackTrigger(animator))
        {
            animator.SetTrigger(AttackTrigger);
        }
    }

    private static bool HasAttackTrigger(Animator targetAnimator)
    {
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == AttackTrigger && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }
}
