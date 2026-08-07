using UnityEngine;

/// <summary>
/// 바라보는 방향에 맞는 클립을 이름으로 직접 재생한다.
/// 상태(대기·뛰기·공격) × 방향(좌·우) 조합마다 전환선을 긋는 대신 Play()로 부르면,
/// 모션이 늘어나도 컨트롤러에 스테이트만 끌어다 놓으면 끝난다.
/// 클립 이름 규칙: Player_&lt;상태&gt;_&lt;방향&gt;
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerAnimator : MonoBehaviour
{
    private const float MoveThresholdSquared = 0.0001f;

    private const string IdleLeft = "Player_Idle_Left";
    private const string IdleRight = "Player_Idle_Right";
    private const string RunLeft = "Player_Run_Left";
    private const string RunRight = "Player_Run_Right";
    private const string IdleAttackLeft = "Player_Idle_Attack_Left";
    private const string IdleAttackRight = "Player_Idle_Attack_Right";
    private const string RunAttackLeft = "Player_Run_Attack_Left";
    private const string RunAttackRight = "Player_Run_Attack_Right";

    [SerializeField, Min(1f)]
    [Tooltip("공격 모션을 클립 길이의 최대 몇 배까지 늘릴지. 아주 느린 무기에서 슬로모션이 되는 걸 막는다.")]
    private float maxAttackStretch = 1.5f;

    private PlayerMovement movement;
    private Animator animator;
    private string currentState;
    private bool facingRight = true;

    private float idleAttackSeconds = 0.33f;
    private float runAttackSeconds = 0.53f;

    private float attackUntil;
    private bool attackWhileRunning;

    // 모션을 이어 붙일지 판단하려면 무시한 공격 요청도 시각을 알아야 한다.
    private float lastAttackTime = float.NegativeInfinity;
    private float lastAttackInterval;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();

        // 클립 길이를 직접 읽어둔다. 프레임 수나 fps를 바꿔도 상수를 같이 고칠 일이 없다.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == IdleAttackRight) idleAttackSeconds = clip.length;
                else if (clip.name == RunAttackRight) runAttackSeconds = clip.length;
            }
        }
    }

    /// <summary>
    /// 공격이 실제로 나갔을 때 부른다. 쿨타임에 막힌 입력으로는 부르지 않는다.
    /// 방향은 공격 방향(화살표키)이라 이동 방향과 다를 수 있다 —
    /// 오른쪽으로 달리면서 왼쪽을 치면 왼쪽 공격 모션이 나와야 한다.
    /// </summary>
    /// <param name="attackInterval">이번 무기의 공격 간격(초). 모션 속도를 여기 맞춘다.</param>
    public void PlayAttack(Vector2 attackDirection, float attackInterval)
    {
        // 무시할 요청이어도 시각은 남긴다 — 모션을 이어 붙일지 판단하는 근거다.
        lastAttackTime = Time.time;
        lastAttackInterval = attackInterval;

        // 방향은 되감기 가드보다 먼저 갱신한다. 스윙 도중에 반대쪽을 쳐도 즉시 돌아봐야 한다.
        // 방향이 바뀌면 아래 Play가 반대쪽 스테이트로 넘어가면서 스윙도 새로 시작된다.
        if (!Mathf.Approximately(attackDirection.x, 0f))
        {
            facingRight = attackDirection.x > 0f;
        }

        bool nowRunning = movement.CurrentDirection.sqrMagnitude > MoveThresholdSquared;

        // 달리며 치는 클립(0.80초)은 공속(기본 검 0.50초)보다 길다. 재생 중에 되감으면
        // 스윙이 중간에서 잘리므로, 끝날 때까지는 새 공격이 모션을 건드리지 않게 둔다.
        // 단 멈춰서 친 공격은 막지 않는다 — 막으면 서서 치는 모션으로 넘어가질 못한다.
        // 공격 판정 자체는 호출한 쪽에서 이미 나갔으므로 전투에는 영향이 없다.
        if (attackWhileRunning && nowRunning && Time.time < attackUntil)
        {
            return;
        }

        // 서서 치는지 달리며 치는지는 방아쇠를 당긴 순간에 고정한다.
        // 스윙 도중에 바꾸면 클립이 처음부터 다시 돌아서 손이 끊겨 보인다.
        attackWhileRunning = nowRunning;
        float clipSeconds = attackWhileRunning ? runAttackSeconds : idleAttackSeconds;

        // 달리며 치는 클립은 다리 그림이 뛰기 모션과 같아서 항상 1배속으로 둔다.
        // 공속에 맞춰 늘리거나 줄이면 다리만 어긋나 미끄러져 보인다.
        // 서서 칠 때는 다리가 멈춰 있으니 공속에 맞춰도 된다 —
        // 빠른 무기는 모션도 빨라지고, 느린 무기에서 슬로모션이 되지 않게 늘어나는 폭만 막는다.
        float duration = clipSeconds;
        if (!attackWhileRunning && attackInterval > 0f)
        {
            duration = Mathf.Min(attackInterval, clipSeconds * maxAttackStretch);
        }

        if (animator != null)
        {
            animator.speed = clipSeconds / duration;
        }

        attackUntil = Time.time + duration;
        LastAttackSeconds = duration;
        currentState = null;   // 같은 클립을 다시 처음부터 돌리려면 비워야 한다
    }

    /// <summary>
    /// 마지막 공격 모션이 실제로 재생되는 시간(초). 손에 든 무기 스윙을 여기 맞춘다 —
    /// 무기가 먼저 끝나고 몸만 계속 휘두르면 따로 노는 게 보인다.
    /// </summary>
    public float LastAttackSeconds { get; private set; }

    private void Update()
    {
        // 공격 중에는 공격 방향이 우선이다. 이동 방향 갱신보다 먼저 빠져나가야
        // PlayAttack이 잡아둔 방향을 이동 입력이 덮어쓰지 않는다.
        if (Time.time < attackUntil)
        {
            Play(attackWhileRunning
                ? (facingRight ? RunAttackRight : RunAttackLeft)
                : (facingRight ? IdleAttackRight : IdleAttackLeft));
            return;
        }

        // 모션이 끝난 순간에도 공격이 이어지고 있으면 곧바로 다시 시작한다.
        // 안 그러면 클립(0.80초)과 공속(0.50초)이 안 맞아떨어지는 만큼 평범한 달리기가 끼어든다.
        if (attackWhileRunning
            && Time.time <= lastAttackTime + lastAttackInterval
            && movement.CurrentDirection.sqrMagnitude > MoveThresholdSquared)
        {
            attackUntil = Time.time + runAttackSeconds;
            currentState = null;
            Play(facingRight ? RunAttackRight : RunAttackLeft);
            return;
        }

        // 공격이 끝나면 재생 속도를 되돌린다.
        // ponytail: animator.speed는 컨트롤러 전체에 걸린다. 이동 모션에도 속도를 걸 일이
        // 생기면 그때 공격 스테이트의 speed 파라미터로 옮기면 된다.
        if (animator != null && !Mathf.Approximately(animator.speed, 1f))
        {
            animator.speed = 1f;
        }

        // 실제로 움직이는 동안에만 방향을 갱신한다. LastMoveDirection은 멈춘 뒤에도
        // 마지막 걷던 방향을 계속 들고 있어서, 서서 공격한 방향을 매 프레임 되돌려버린다.
        // 위아래로만 움직이면 x가 0이라 이 조건에서 걸러져 마지막 방향이 유지된다.
        float x = movement.CurrentDirection.x;
        if (!Mathf.Approximately(x, 0f))
        {
            facingRight = x > 0f;
        }

        // 입력이 살아 있으면 뛰기. CurrentDirection은 입력을 뗀 프레임에 바로 0이 된다.
        bool running = movement.CurrentDirection.sqrMagnitude > MoveThresholdSquared;

        Play(running
            ? (facingRight ? RunRight : RunLeft)
            : (facingRight ? IdleRight : IdleLeft));
    }

    private void Play(string state)
    {
        if (animator == null || state == currentState)
        {
            return;
        }

        currentState = state;

        // 인자 하나짜리 Play는 이미 그 스테이트에 있으면 재생 위치를 유지한다.
        // 그러면 연속 공격 때 끝나 있던 마지막 프레임에 멈춘 채로 남는다. 항상 0에서 다시 시작시킨다.
        animator.Play(state, 0, 0f);
    }
}
