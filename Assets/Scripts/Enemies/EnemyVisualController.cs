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

    /// <summary>
    /// 한 번 움직임을 감지하면 이만큼은 이동으로 친다. 물리 스텝(기본 0.02초)이 없는
    /// 프레임에는 위치 변화가 0으로 잡히는데, 그때마다 대기로 떨어지면 걷기 클립이
    /// 계속 처음부터 다시 돌아 멈춘 것처럼 보인다.
    /// </summary>
    private const float MoveHoldSeconds = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool directional;
    private bool hasMoveStates;
    private float attackSeconds;

    // 대기·이동 위에 잠깐 덮어 재생하는 상태(공격·돌진 준비 등). 이름을 미리 풀어 두면
    // 매 프레임 문자열을 잇지 않아도 된다.
    private string overrideLeftState;
    private string overrideRightState;
    private float overrideUntil;

    private Transform bodyTransform;
    private Vector3 lastPosition;
    private bool facingRight = true;
    private string currentState;
    private float movingUntil;

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
        // 공격 모션 길이를 클립에서 직접 읽는다. 프레임 수나 fps를 바꿔도 코드는 그대로다.
        attackSeconds = ClipSecondsOf(AttackRight);
        overrideUntil = 0f;

        // 컨트롤러를 갈아끼우는 경우(보스 페이즈 전환) 이전 스테이트 이름이 남아 있으면
        // 같은 이름일 때 Play가 건너뛰어 새 컨트롤러가 기본 스테이트에 멈춘다.
        currentState = null;

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

        // 덮어쓰기 구간(공격·돌진 준비 등)에는 이동·대기 모션을 무시한다.
        if (Time.time < overrideUntil)
        {
            Play(facingRight ? overrideRightState : overrideLeftState);
            return;
        }

        // 덮어쓰기가 끝나면 배속을 되돌린다.
        if (!Mathf.Approximately(animator.speed, 1f))
        {
            animator.speed = 1f;
        }

        // MovePosition으로 움직여서 velocity를 믿을 수 없다. 실제 위치 변화로 판단한다.
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        if (speed > MoveSpeedThreshold)
        {
            movingUntil = Time.time + MoveHoldSeconds;

            // 세로로만 움직이면 x가 0에 가까워 마지막으로 본 방향을 유지한다.
            if (Mathf.Abs(delta.x) > Mathf.Epsilon)
            {
                facingRight = delta.x > 0f;
            }
        }

        bool moving = Time.time < movingUntil;

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

        // 좌우 공격 모션이 있으면 그걸 클립 길이만큼 재생한다. 방향은 지금 보는 쪽을 유지.
        if (PlayOverride("Attack", Vector2.zero, attackSeconds))
        {
            return;
        }

        // 전환선으로 짠 옛 방식 컨트롤러도 그대로 지원한다.
        if (HasAttackTrigger(animator))
        {
            animator.SetTrigger(AttackTrigger);
        }
    }

    /// <summary>
    /// <paramref name="baseName"/>_Left / _Right 상태를 <paramref name="seconds"/> 동안
    /// 대기·이동 모션 위에 덮어 재생한다.
    ///
    /// 클립이 더 짧으면 남는 시간은 마지막 프레임을 붙잡고 있는다(루프 OFF 기준) —
    /// 돌진 준비처럼 "자세를 잡고 멈춰 있는" 연출이 이걸로 나온다.
    /// 더 길면 잘리지 않게 배속만 올린다. 늘리지는 않는다.
    /// </summary>
    /// <param name="direction">바라볼 방향. 좌우 성분이 없으면 지금 방향을 지킨다.</param>
    /// <returns>그 좌우 상태가 컨트롤러에 있어서 실제로 걸었는지.</returns>
    public bool PlayOverride(string baseName, Vector2 direction, float seconds)
    {
        if (animator == null || seconds <= 0f || string.IsNullOrEmpty(baseName))
        {
            return false;
        }

        string left = baseName + "_Left";
        string right = baseName + "_Right";
        if (!HasState(left) || !HasState(right))
        {
            return false;
        }

        if (!Mathf.Approximately(direction.x, 0f))
        {
            facingRight = direction.x > 0f;
        }

        float clipSeconds = ClipSecondsOf(right);
        animator.speed = clipSeconds > seconds ? clipSeconds / seconds : 1f;

        overrideLeftState = left;
        overrideRightState = right;
        overrideUntil = Time.time + seconds;
        currentState = null;   // 같은 클립을 다시 처음부터 돌리려면 비워야 한다
        return true;
    }

    /// <summary>이름이 이 상태로 끝나는 클립의 길이(초). 없으면 0.</summary>
    private float ClipSecondsOf(string stateName)
    {
        RuntimeAnimatorController controller = animator == null ? null : animator.runtimeAnimatorController;
        if (controller == null)
        {
            return 0f;
        }

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name.EndsWith(stateName))
            {
                return clip.length;
            }
        }

        return 0f;
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
