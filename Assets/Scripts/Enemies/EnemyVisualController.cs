using UnityEngine;

public sealed class EnemyVisualController : MonoBehaviour
{
    private const string AttackTrigger = "Attack";

    // 좌우 모션을 가진 적이 쓰는 스테이트 이름.
    // Idle 좌우만 있으면 방향 전환은 되고, Move 좌우까지 있으면 이동 모션도 쓴다.
    private const string IdleLeft = "Idle_Left";
    private const string IdleRight = "Idle_Right";
    private const string MoveLeft = "Move_Left";
    private const string MoveRight = "Move_Right";
    private const string AttackLeft = "Attack_Left";
    private const string AttackRight = "Attack_Right";

    /// <summary>이 속도(초당 유닛)를 넘으면 이동으로 본다. 물리 흔들림에 반응하지 않을 만큼 작다.</summary>
    private const float MoveSpeedThreshold = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool directional;
    private bool hasMoveStates;
    private bool hasAttackStates;
    private float attackSeconds;
    private float attackUntil;
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

        // Idle 좌우가 있으면 방향 전환 대상이다. 고정형(나무탑)은 Move 모션이 없어도 된다.
        // 기존 적들(단일 스프라이트, 컨트롤러 없음)은 여기서 걸러져 동작이 그대로 유지된다.
        directional = HasState(IdleLeft) && HasState(IdleRight);
        hasMoveStates = HasState(MoveLeft) && HasState(MoveRight);
        hasAttackStates = HasState(AttackLeft) && HasState(AttackRight);

        // 공격 모션 길이를 클립에서 직접 읽는다. 프레임 수나 fps를 바꿔도 코드는 그대로다.
        attackSeconds = 0f;
        foreach (AnimationClip clip in animatorController.animationClips)
        {
            if (clip.name.EndsWith(AttackRight))
            {
                attackSeconds = clip.length;
                break;
            }
        }

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

        // 공격 중에는 이동·대기 모션을 덮어쓴다. 방향은 PlayAttack이 잡아둔 값을 그대로 쓴다.
        if (Time.time < attackUntil)
        {
            Play(facingRight ? AttackRight : AttackLeft);
            return;
        }

        // MovePosition으로 움직여서 velocity를 믿을 수 없다. 실제 위치 변화로 판단한다.
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        bool moving = speed > MoveSpeedThreshold;

        // 세로로만 움직이면 x가 0에 가까워 마지막으로 본 방향을 유지한다.
        if (moving && Mathf.Abs(delta.x) > Mathf.Epsilon)
        {
            facingRight = delta.x > 0f;
        }

        Play(moving && hasMoveStates
            ? (facingRight ? MoveRight : MoveLeft)
            : (facingRight ? IdleRight : IdleLeft));
    }

    /// <summary>
    /// 지정한 방향을 보게 한다. 고정형 적처럼 이동량으로는 방향을 알 수 없을 때 쓴다 —
    /// 나무탑은 공격할 때 플레이어 쪽을 본다.
    /// 좌우 성분이 없으면(정확히 위/아래) 마지막으로 본 방향을 지킨다.
    /// </summary>
    public void FaceTowards(Vector2 direction)
    {
        if (!Mathf.Approximately(direction.x, 0f))
        {
            facingRight = direction.x > 0f;
        }
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
        if (animator == null)
        {
            return;
        }

        // 좌우 공격 모션이 있으면 그걸 재생한다. 클립 길이만큼 대기·이동 모션을 덮는다.
        if (hasAttackStates && attackSeconds > 0f)
        {
            attackUntil = Time.time + attackSeconds;
            currentState = null;   // 같은 클립을 다시 처음부터 돌리려면 비워야 한다
            return;
        }

        // 전환선으로 짠 옛 방식 컨트롤러도 그대로 지원한다.
        if (HasAttackTrigger(animator))
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
