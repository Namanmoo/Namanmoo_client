using UnityEditor;
using UnityEngine;

/// <summary>
/// 무기 손 키프레임 작업용 리그 프리팹을 만든다.
///
/// 런타임 플레이어는 PlayerFactory가 코드로 조립해서 씬에 편집할 대상이 없다.
/// 이 리그는 그 계층(Player Visual + Weapon Hand)을 그대로 흉내 내므로,
/// 프리팹을 열고 Animation 창에서 몸 클립에 "Weapon Hand"의 localPosition
/// 커브를 찍으면 게임에서도 같은 경로로 바인딩돼 손 위치가 움직인다.
/// </summary>
public static class WeaponHandRigBuilder
{
    private const string PlayerSpritePath =
        "Assets/Player/Animation/Sword/Idle/Right/Frames/player_idle0000.png";
    private const string SwordSpritePath = "Assets/Weapons/sword.png";
    private const string ControllerPath = "Assets/Resources/Player/PlayerVisual.controller";
    private const string PrefabPath = "Assets/Player/Animation/WeaponHandRig.prefab";

    /// <summary>플레이어 몸 그림의 정렬 순서 — PlayerFactory가 쓰는 값과 같아야 한다.</summary>
    private const int BodySortingOrder = 4;

    [MenuItem("Tools/NaManMoo/Build Weapon Hand Rig")]
    public static void Build()
    {
        Sprite playerSprite = LoadRequired<Sprite>(PlayerSpritePath);
        Sprite swordSprite = LoadRequired<Sprite>(SwordSpritePath);
        var controller = LoadRequired<RuntimeAnimatorController>(ControllerPath);

        var root = new GameObject("Weapon Hand Rig");
        try
        {
            // PlayerFactory.Create와 같은 셈법 — 리그에서 보이는 크기가 게임과 같아야
            // 눈으로 맞춘 손 위치를 그대로 믿을 수 있다.
            var visualObject = new GameObject("Player Visual");
            visualObject.transform.SetParent(root.transform, false);
            float visualScale = PlayerFactory.VisualHeight / playerSprite.bounds.size.y;
            visualObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);

            SpriteRenderer body = visualObject.AddComponent<SpriteRenderer>();
            body.sprite = playerSprite;
            body.sortingOrder = BodySortingOrder;

            Animator animator = visualObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // PlayerWeaponVisual.EnsureRenderer와 같은 자리·크기로 시작한다.
            // 커브가 이 localPosition을 프레임마다 덮어쓰게 된다.
            var handObject = new GameObject("Weapon Hand");
            handObject.transform.SetParent(visualObject.transform, false);
            handObject.transform.localPosition =
                PlayerWeaponVisual.DefaultHandOffset / visualScale;
            float weaponScale = PlayerWeaponVisual.WeaponScale / visualScale;
            handObject.transform.localScale = new Vector3(weaponScale, weaponScale, 1f);

            SpriteRenderer weapon = handObject.AddComponent<SpriteRenderer>();
            weapon.sprite = swordSprite;
            weapon.sortingOrder = PlayerWeaponVisual.SortingOrder;
            weapon.transform.localRotation = Quaternion.Euler(
                0f, 0f, PlayerWeaponVisual.AngleFor(PlayerWeaponVisual.RestTipDirection));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
            Debug.Log($"무기 손 리그 저장: {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new System.InvalidOperationException(
                $"리그를 만들려면 {path} 가 필요합니다.");
        }

        return asset;
    }
}
