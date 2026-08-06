using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기고 화면을 코드로 조립한다.
///
/// 무기 만들기 화면과 달리 배경 목업이 없어 UI를 직접 그린다. 종이 느낌만 맞춰
/// 흰 카드 + 검은 테두리로 두고, 카드 칸은 <see cref="WeaponVaultController.CardCount"/>개를
/// 미리 만들어 둔다 — 런타임에 UI를 새로 만들면 WebGL에서 첫 표시가 늦어진다.
/// </summary>
public static class WeaponVaultSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/WeaponVault.unity";

    private const string FontPath = "Assets/Resources/Fonts/Gaegu-Regular.ttf";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    private static readonly Color Paper = new Color(0.957f, 0.949f, 0.933f, 1f);
    private static readonly Color Ink = new Color(0.13f, 0.13f, 0.15f, 1f);
    private static readonly Color CardFace = Color.white;
    private static readonly Color Accent = new Color(0.16f, 0.62f, 0.36f, 1f);
    private static readonly Color Danger = new Color(0.75f, 0.22f, 0.18f, 1f);

    // 4열 2행 격자
    private const int Columns = 4;
    private const float GridLeft = 0.045f;
    private const float GridRight = 0.955f;
    private const float GridTop = 0.155f;
    private const float GridBottom = 0.845f;
    private const float CardGap = 0.012f;

    [MenuItem("Tools/NaManMoo/Build Weapon Vault")]
    public static void Build()
    {
        Font font = LoadFont();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();

        // 종이 바탕
        CreateSolid(canvas.transform, "Paper", Rect.MinMaxRect(0f, 0f, 1f, 1f), Paper);

        Text title = CreateText(canvas.transform, "Title", font, "무기고", 64);
        SetArea((RectTransform)title.transform, Rect.MinMaxRect(0.04f, 0.03f, 0.5f, 0.12f));
        title.color = Ink;
        title.alignment = TextAnchor.MiddleLeft;

        Text status = CreateText(canvas.transform, "Status", font, string.Empty, 30);
        SetArea((RectTransform)status.transform, Rect.MinMaxRect(0.30f, 0.045f, 0.72f, 0.115f));
        status.color = new Color(0.35f, 0.35f, 0.38f, 1f);
        status.alignment = TextAnchor.MiddleLeft;

        var controllerObject = new GameObject(
            "Weapon Vault Controller", typeof(WeaponVaultController));
        WeaponVaultController controller =
            controllerObject.GetComponent<WeaponVaultController>();

        Button back = CreateLabeledButton(
            canvas.transform, font, "Back Button", "← 돌아가기",
            Rect.MinMaxRect(0.74f, 0.035f, 0.86f, 0.115f), new Color(0.42f, 0.42f, 0.46f, 1f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            back.onClick, controller.GoBackToForge);

        Button forge = CreateLabeledButton(
            canvas.transform, font, "Forge Button", "새로 만들기",
            Rect.MinMaxRect(0.87f, 0.035f, 0.96f, 0.115f), Accent);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            forge.onClick, controller.GoToForge);

        CardParts parts = CreateCards(canvas.transform, font, controller);
        EditPanelParts edit = CreateEditPanel(canvas.transform, font, controller);
        WireController(controller, status, back, forge, parts, edit);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Debug.Log($"[WeaponVaultSceneBuilder] {ScenePath} 생성 완료");
    }

    // ── 카드 격자 ──────────────────────────────────────────

    private struct CardParts
    {
        public GameObject[] Cards;
        public RawImage[] Images;
        public Text[] Names;
        public Text[] Details;
        public Button[] Equip;
        public Button[] Delete;
    }

    private struct EditPanelParts
    {
        public GameObject Panel;
        public RawImage Image;
        public WeaponPointPicker Picker;
        public Text Title;
        public RectTransform[] Markers;
        public Image[] Highlights;
    }

    private static CardParts CreateCards(
        Transform parent, Font font, WeaponVaultController controller)
    {
        int count = WeaponVaultController.CardCount;
        int rows = Mathf.CeilToInt(count / (float)Columns);

        var parts = new CardParts
        {
            Cards = new GameObject[count],
            Images = new RawImage[count],
            Names = new Text[count],
            Details = new Text[count],
            Equip = new Button[count],
            Delete = new Button[count]
        };

        float cellWidth = (GridRight - GridLeft) / Columns;
        float cellHeight = (GridBottom - GridTop) / rows;

        for (int index = 0; index < count; index++)
        {
            int column = index % Columns;
            int row = index / Columns;
            Rect area = Rect.MinMaxRect(
                GridLeft + column * cellWidth + CardGap * 0.5f,
                GridTop + row * cellHeight + CardGap * 0.5f,
                GridLeft + (column + 1) * cellWidth - CardGap * 0.5f,
                GridTop + (row + 1) * cellHeight - CardGap * 0.5f);

            // 테두리 → 흰 면 순서로 겹쳐 손그림 카드 느낌만 낸다
            var card = new GameObject($"Card {index}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            SetArea((RectTransform)card.transform, area);
            card.GetComponent<Image>().color = Ink;

            Image face = CreateSolid(card.transform, "Face", null, CardFace);
            RectTransform faceRect = (RectTransform)face.transform;
            Stretch(faceRect);
            faceRect.offsetMin = new Vector2(4f, 4f);
            faceRect.offsetMax = new Vector2(-4f, -4f);

            // 카드 안도 전부 SetArea(좌상단 원점)로 맞춘다 —
            // 이미지만 Unity 앵커(하단 원점)를 썼다가 이름·설명과 어긋났다.
            var imageObject = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(card.transform, false);
            SetArea(
                (RectTransform)imageObject.transform,
                Rect.MinMaxRect(0.06f, 0.06f, 0.94f, 0.56f));
            RawImage raw = imageObject.GetComponent<RawImage>();
            raw.raycastTarget = false;

            Text name = CreateText(card.transform, "Name", font, string.Empty, 30);
            SetArea((RectTransform)name.transform, Rect.MinMaxRect(0.04f, 0.58f, 0.96f, 0.70f));
            name.color = Ink;
            name.alignment = TextAnchor.MiddleCenter;

            Text detail = CreateText(card.transform, "Detail", font, string.Empty, 24);
            SetArea((RectTransform)detail.transform, Rect.MinMaxRect(0.04f, 0.70f, 0.96f, 0.86f));
            detail.color = new Color(0.38f, 0.38f, 0.42f, 1f);
            detail.alignment = TextAnchor.MiddleCenter;

            Button equip = CreateLabeledButton(
                card.transform, font, "Equip", "장착",
                Rect.MinMaxRect(0.05f, 0.87f, 0.40f, 0.97f), Accent);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                equip.onClick, controller.Equip, index);

            // 그립·중심·끝을 다시 찍는 화면을 연다
            Button edit = CreateLabeledButton(
                card.transform, font, "Edit", "수정",
                Rect.MinMaxRect(0.43f, 0.87f, 0.66f, 0.97f),
                new Color(0.42f, 0.42f, 0.46f, 1f));
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                edit.onClick, controller.BeginEdit, index);

            Button delete = CreateLabeledButton(
                card.transform, font, "Delete", "삭제",
                Rect.MinMaxRect(0.69f, 0.87f, 0.95f, 0.97f), Danger);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                delete.onClick, controller.Delete, index);

            card.SetActive(false);

            parts.Cards[index] = card;
            parts.Images[index] = raw;
            parts.Names[index] = name;
            parts.Details[index] = detail;
            parts.Equip[index] = equip;
            parts.Delete[index] = delete;
        }

        return parts;
    }

    /// <summary>
    /// 기준점 수정 화면 — 화면을 덮는 어두운 막 위에 그림을 크게 띄우고,
    /// 그립·중심·끝을 골라 다시 찍는다. 평소에는 꺼 둔다.
    /// </summary>
    private static EditPanelParts CreateEditPanel(
        Transform parent, Font font, WeaponVaultController controller)
    {
        // 뒤 카드가 눌리지 않게 화면 전체를 덮는다
        var panel = new GameObject("Edit Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Stretch((RectTransform)panel.transform);
        Image scrim = panel.GetComponent<Image>();
        scrim.color = new Color(0f, 0f, 0f, 0.55f);
        scrim.raycastTarget = true;

        Text title = CreateText(panel.transform, "Title", font, string.Empty, 40);
        SetArea((RectTransform)title.transform, Rect.MinMaxRect(0.20f, 0.075f, 0.80f, 0.145f));
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;

        // 그림 자리 — 기준 해상도(1920×1080)에서 정사각형이 되게 잡는다
        Rect imageArea = Rect.MinMaxRect(0.32f, 0.16f, 0.68f, 0.80f);
        CreateSolid(panel.transform, "Image Border", Grow(imageArea, 0.004f, 0.007f), Ink);
        CreateSolid(panel.transform, "Image Face", imageArea, CardFace);

        var imageObject = new GameObject(
            "Weapon Image", typeof(RectTransform), typeof(RawImage), typeof(WeaponPointPicker));
        imageObject.transform.SetParent(panel.transform, false);
        SetArea((RectTransform)imageObject.transform, imageArea);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = true;  // 눌러야 점이 옮겨진다

        // 마커 — 그림의 자식으로 두고 앵커만 움직인다 (RefreshEditMarkers)
        var markers = new RectTransform[3];
        var markerColors = new[]
        {
            new Color(0.98f, 0.35f, 0.1f, 0.9f),   // 그립
            new Color(0.15f, 0.5f, 0.95f, 0.9f),   // 중심
            new Color(0.1f, 0.75f, 0.3f, 0.9f)     // 끝
        };
        var markerNames = new[] { "Grip Marker", "Center Marker", "Tip Marker" };
        for (int index = 0; index < markers.Length; index++)
        {
            var markerObject = new GameObject(
                markerNames[index], typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(imageObject.transform, false);
            var rect = (RectTransform)markerObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(20f, 20f);
            Image markerImage = markerObject.GetComponent<Image>();
            markerImage.color = markerColors[index];
            markerImage.raycastTarget = false;
            markers[index] = rect;
        }

        // 어느 점을 옮길지 고르는 버튼 줄 + 선택 밑줄
        var highlights = new Image[3];
        var pointLabels = new[] { "손잡이", "중심", "끝" };
        var pointButtonColors = new[]
        {
            new Color(0.55f, 0.35f, 0.15f, 1f),
            new Color(0.15f, 0.4f, 0.7f, 1f),
            new Color(0.1f, 0.55f, 0.25f, 1f)
        };
        for (int index = 0; index < 3; index++)
        {
            float left = 0.32f + index * 0.125f;
            Rect buttonArea = Rect.MinMaxRect(left, 0.815f, left + 0.11f, 0.875f);
            Button pointButton = CreateLabeledButton(
                panel.transform, font, $"{markerNames[index]} Button",
                pointLabels[index], buttonArea, pointButtonColors[index]);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                pointButton.onClick, controller.SelectEditPoint, index);

            Image underline = CreateSolid(
                panel.transform, $"{markerNames[index]} Selected",
                Rect.MinMaxRect(buttonArea.xMin, 0.878f, buttonArea.xMax, 0.888f),
                new Color(0.98f, 0.55f, 0.1f, 1f));
            underline.enabled = false;
            highlights[index] = underline;
        }

        Button save = CreateLabeledButton(
            panel.transform, font, "Save Button", "저장",
            Rect.MinMaxRect(0.38f, 0.90f, 0.49f, 0.96f), Accent);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            save.onClick, controller.SaveEdit);

        Button cancel = CreateLabeledButton(
            panel.transform, font, "Cancel Button", "취소",
            Rect.MinMaxRect(0.51f, 0.90f, 0.62f, 0.96f),
            new Color(0.42f, 0.42f, 0.46f, 1f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            cancel.onClick, controller.CancelEdit);

        panel.SetActive(false);

        return new EditPanelParts
        {
            Panel = panel,
            Image = image,
            Picker = imageObject.GetComponent<WeaponPointPicker>(),
            Title = title,
            Markers = markers,
            Highlights = highlights
        };
    }

    /// <summary>좌상단 원점 비율 영역을 사방으로 조금 키운다 — 테두리용.</summary>
    private static Rect Grow(Rect area, float horizontal, float vertical)
    {
        return Rect.MinMaxRect(
            area.xMin - horizontal, area.yMin - vertical,
            area.xMax + horizontal, area.yMax + vertical);
    }

    private static void WireController(
        WeaponVaultController controller,
        Text status,
        Button back,
        Button forge,
        CardParts parts,
        EditPanelParts edit)
    {
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("forgeButton").objectReferenceValue = forge;

        AssignArray(serialized, "cards", parts.Cards);
        AssignArray(serialized, "cardImages", parts.Images);
        AssignArray(serialized, "cardNames", parts.Names);
        AssignArray(serialized, "cardDetails", parts.Details);
        AssignArray(serialized, "equipButtons", parts.Equip);
        AssignArray(serialized, "deleteButtons", parts.Delete);

        serialized.FindProperty("editPanel").objectReferenceValue = edit.Panel;
        serialized.FindProperty("editImage").objectReferenceValue = edit.Image;
        serialized.FindProperty("editPicker").objectReferenceValue = edit.Picker;
        serialized.FindProperty("editTitle").objectReferenceValue = edit.Title;
        AssignArray(serialized, "editMarkers", edit.Markers);
        AssignArray(serialized, "editPointHighlights", edit.Highlights);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignArray(SerializedObject serialized, string name, Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    // ── 공용 (무기 만들기 빌더와 같은 방식) ────────────────────

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Paper;
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject(
            "Weapon Vault Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Image CreateSolid(Transform parent, string name, Rect? area, Color color)
    {
        var solid = new GameObject(name, typeof(RectTransform), typeof(Image));
        solid.transform.SetParent(parent, false);
        if (area.HasValue)
        {
            SetArea((RectTransform)solid.transform, area.Value);
        }

        Image image = solid.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateLabeledButton(
        Transform parent, Font font, string name, string label, Rect area, Color color)
    {
        var buttonObject = new GameObject(
            name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetArea((RectTransform)buttonObject.transform, area);
        buttonObject.GetComponent<Image>().color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        Text text = CreateText(buttonObject.transform, "Label", font, label, 28);
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static Text CreateText(
        Transform parent, string name, Font font, string content, int fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Stretch((RectTransform)textObject.transform);

        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>좌상단 원점 비율 좌표를 Unity 앵커(좌하단 원점)로 옮긴다.</summary>
    private static void SetArea(RectTransform rect, Rect area)
    {
        rect.anchorMin = new Vector2(area.xMin, 1f - area.yMax);
        rect.anchorMax = new Vector2(area.xMax, 1f - area.yMin);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Font LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            throw new System.InvalidOperationException($"폰트가 없습니다: {FontPath}");
        }

        return font;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == ScenePath))
        {
            return;
        }

        // 무기 만들기 바로 뒤 — 두 화면이 서로를 오간다
        int forgeIndex = scenes.FindIndex(
            scene => scene.path == WeaponForgeSceneBuilder.ScenePath);
        scenes.Insert(forgeIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
