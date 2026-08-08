using UnityEditor;
using UnityEngine;

/// <summary>
/// 무기 손 키프레임 작업용 리그 프리팹을 만든다.
///
/// 런타임 플레이어는 PlayerFactory가 코드로 조립해서 씬에 편집할 대상이 없다.
/// 이 리그는 그 계층(Player Visual + Weapon Hand + Weapon)을 그대로 흉내 내므로,
/// 프리팹을 열고 Animation 창에서 몸 클립에 "Weapon Hand"의 localPosition(그립
/// 위치)·localRotation(칼끝 방향) 커브를 찍으면 게임에서도 같은 경로로 바인딩된다.
/// 그립(초록)·칼끝(빨강) 마커가 붙어 있어 두 점이 어디 찍히는지 눈으로 확인한다.
/// </summary>
public static class WeaponHandRigBuilder
{
    private const string PlayerSpritePath =
        "Assets/Player/Animation/Sword/Idle/Right/Frames/player_idle0000.png";
    private const string ControllerPath = "Assets/Resources/Player/PlayerVisual.controller";
    private const string PrefabPath = "Assets/Player/Animation/WeaponHandRig.prefab";

    /// <summary>
    /// 리그에 들려 줄 가이드 무기 그림 — 없으면 빌더가 그려서 만든다.
    /// 실제 무기(sword.png 등)는 pivot이 그립이 아니거나 누워 있어서 기준으로
    /// 삼기 혼란스럽다. 가이드는 커브 규약 그대로다: pivot=아래(그립), 위=끝.
    /// </summary>
    private const string GuideSpritePath = "Assets/Player/Animation/WeaponGuide.png";

    private const int GuideWidth = 24;
    private const int GuideHeight = 96;

    /// <summary>가이드 세로 96px가 세계 2유닛이 되는 배율 — 실제 무기와 비슷한 길이.</summary>
    private const float GuidePixelsPerUnit = 48f;

    /// <summary>플레이어 몸 그림의 정렬 순서 — PlayerFactory가 쓰는 값과 같아야 한다.</summary>
    private const int BodySortingOrder = 4;

    [MenuItem("Tools/NaManMoo/Build Weapon Hand Rig")]
    public static void Build()
    {
        Sprite playerSprite = LoadRequired<Sprite>(PlayerSpritePath);
        Sprite guideSprite = EnsureGuideSprite();
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
            // 커브가 이 localPosition(그립)·localRotation(칼끝 방향)을 프레임마다 덮어쓴다.
            var handObject = new GameObject("Weapon Hand");
            handObject.transform.SetParent(visualObject.transform, false);
            handObject.transform.localPosition =
                PlayerWeaponVisual.DefaultHandOffset / visualScale;
            handObject.transform.localRotation = Quaternion.Euler(
                0f, 0f, PlayerWeaponVisual.AngleFor(PlayerWeaponVisual.RestTipDirection));
            float weaponScale = PlayerWeaponVisual.WeaponScale / visualScale;
            handObject.transform.localScale = new Vector3(weaponScale, weaponScale, 1f);

            // 렌더러는 자식에 — 런타임과 같은 계층. 무기별 축 보정이 여기 얹힌다.
            var weaponObject = new GameObject("Weapon");
            weaponObject.transform.SetParent(handObject.transform, false);
            SpriteRenderer weapon = weaponObject.AddComponent<SpriteRenderer>();
            weapon.sprite = guideSprite;
            weapon.sortingOrder = PlayerWeaponVisual.SortingOrder;

            // 그립 = Weapon Hand의 원점, 칼끝 = 그립에서 스프라이트 위쪽 끝까지.
            // 키를 찍을 때 두 점이 몸 어디에 놓이는지 눈으로 확인하는 용도다.
            AddMarker(handObject.transform, "Grip Marker", Vector3.zero, Color.green);
            AddMarker(
                weaponObject.transform, "Tip Marker",
                Vector3.up * guideSprite.bounds.max.y, Color.red);

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

    /// <summary>가이드 무기 그림을 불러오고, 없으면 그려서 에셋으로 만든다.</summary>
    private static Sprite EnsureGuideSprite()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(GuideSpritePath) == null)
        {
            System.IO.File.WriteAllBytes(
                GuideSpritePath, DrawGuideTexture().EncodeToPNG());
            AssetDatabase.ImportAsset(GuideSpritePath);
        }

        // 임포트 설정은 만들 때 한 번이 아니라 매번 확인한다 — 프로젝트 기본
        // 임포트 설정(프리셋)이 Multiple 모드를 끼워 넣으면 상위 pivot이 무시되고
        // 시트 안 스프라이트의 가운데 pivot이 쓰여서 그립이 무기 한가운데로 간다.
        var importer = (TextureImporter)AssetImporter.GetAtPath(GuideSpritePath);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        Vector2 gripPivot = new Vector2(0.5f, 0f);
        bool needsFix = settings.textureType != TextureImporterType.Sprite
            || settings.spriteMode != (int)SpriteImportMode.Single
            || settings.spriteAlignment != (int)SpriteAlignment.Custom
            || settings.spritePivot != gripPivot
            || settings.spritePixelsPerUnit != GuidePixelsPerUnit;
        if (needsFix)
        {
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            // pivot = 아래 가운데 — 그린 무기의 그립이 pivot으로 구워지는 규약과 같다
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = gripPivot;
            settings.spritePixelsPerUnit = GuidePixelsPerUnit;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return LoadRequired<Sprite>(GuideSpritePath);
    }

    /// <summary>
    /// 세로로 곧은 가이드 무기: 아래 1/4은 어두운 손잡이, 그 위는 밝은 날,
    /// 꼭대기는 뾰족한 끝. 어디가 그립이고 어디가 끝인지 그림만 봐도 읽힌다.
    /// </summary>
    private static Texture2D DrawGuideTexture()
    {
        var texture = new Texture2D(GuideWidth, GuideHeight, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color handle = new Color(0.35f, 0.23f, 0.12f);
        Color blade = new Color(0.85f, 0.87f, 0.9f);

        int handleTop = GuideHeight / 4;
        int tipBase = GuideHeight - GuideWidth / 2;
        int center = GuideWidth / 2;
        for (int y = 0; y < GuideHeight; y++)
        {
            // 위로 갈수록 좁아지는 끝, 그 아래는 일정한 폭의 몸통
            int halfWidth = y < tipBase
                ? GuideWidth / 4
                : Mathf.Max(1, (GuideHeight - y) * GuideWidth / 4 / (GuideHeight - tipBase));
            for (int x = 0; x < GuideWidth; x++)
            {
                bool inside = Mathf.Abs(x - center) <= halfWidth;
                texture.SetPixel(x, y, !inside ? clear : y < handleTop ? handle : blade);
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 리그에서만 보이는 점 표시. 커브를 찍는 대상이 아니라 눈금일 뿐이라
    /// 런타임 계층에는 없다 — 커브 경로("Weapon Hand")와도 무관하다.
    /// </summary>
    private static void AddMarker(
        Transform parent, string name, Vector3 localPosition, Color color)
    {
        var markerObject = new GameObject(name);
        markerObject.transform.SetParent(parent, false);
        markerObject.transform.localPosition = localPosition;
        markerObject.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        SpriteRenderer marker = markerObject.AddComponent<SpriteRenderer>();
        marker.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        marker.color = color;
        marker.sortingOrder = PlayerWeaponVisual.SortingOrder + 2;
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
