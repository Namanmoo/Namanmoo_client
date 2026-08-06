using UnityEngine;

/// <summary>
/// 플레이어 오브젝트를 조립한다. Stage1 씬 빌더와 런타임 부트스트랩이 같은 코드를
/// 복사해 갖고 있었고, 던전이 세 번째가 될 참이었다 — 콜라이더 반지름이나 컴포넌트
/// 하나를 한 곳만 고치는 일이 반드시 생기는 지점이다.
/// </summary>
public static class PlayerFactory
{
    public const float VisualHeight = 4f;
    public const float ColliderRadius = 1f;

    private const string AnimatorResourcePath = "Player/PlayerVisual";

    /// <summary>
    /// 플레이어와 딸린 UI(핫바·체력)를 만든다.
    /// <paramref name="uiParent"/>가 null이면 UI는 씬 루트에 붙는다.
    /// </summary>
    public static GameObject Create(
        Transform parent,
        Transform uiParent,
        Vector3 position,
        Sprite playerSprite,
        Sprite swordSprite,
        Sprite axeSprite,
        Sprite hotbarBackground,
        Sprite healthHeart,
        bool useSampleLoadout = true)
    {
        if (playerSprite == null)
        {
            throw new System.ArgumentNullException(nameof(playerSprite));
        }

        var playerObject = new GameObject("Player") { tag = "Player" };
        if (parent != null)
        {
            playerObject.transform.SetParent(parent, false);
        }

        playerObject.transform.position = position;

        var visualObject = new GameObject("Player Visual");
        visualObject.transform.SetParent(playerObject.transform, false);
        float visualScale = VisualHeight / playerSprite.bounds.size.y;
        visualObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = playerSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 4;

        // 클립이 m_Sprite를 빈 경로로 바인딩하므로 Animator는 SpriteRenderer와 같은
        // 오브젝트에 있어야 한다.
        var controller = Resources.Load<RuntimeAnimatorController>(AnimatorResourcePath);
        if (controller == null)
        {
            throw new System.InvalidOperationException(
                $"Missing player animator controller at Resources/{AnimatorResourcePath}");
        }

        Animator animator = visualObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
        collider.radius = ColliderRadius;

        playerObject.AddComponent<PlayerMovement>();
        playerObject.AddComponent<PlayerAnimator>();
        PlayerSwordShooter shooter = playerObject.AddComponent<PlayerSwordShooter>();
        shooter.SwordSprite = swordSprite;
        PlayerAxeAttacker axeAttacker = playerObject.AddComponent<PlayerAxeAttacker>();
        axeAttacker.AxeSprite = axeSprite;

        Stage1ItemHotbarSetup.Create(
            playerObject, uiParent, hotbarBackground, swordSprite, axeSprite, useSampleLoadout);
        Stage1PlayerHealthSetup.Create(playerObject, uiParent, healthHeart);

        PlayerHealth health = playerObject.GetComponent<PlayerHealth>();
        PlayerDamageFlash damageFlash =
            playerObject.AddComponent<PlayerDamageFlash>();
        damageFlash.Initialize(health, renderer);

        PlayerDash dash = playerObject.GetComponent<PlayerDash>();
        if (dash == null)
        {
            dash = playerObject.AddComponent<PlayerDash>();
        }
        dash.ConfigureVisual(renderer);

        return playerObject;
    }
}
