using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기 만들기 화면을 코드로 조립한다. TitleSceneBuilder와 같은 방식 —
/// 목업 이미지를 배경으로 깔고 그 위에 투명한 상호작용 요소를 정규화 좌표로 올린다.
///
/// 좌표는 Assets/UI/WeaponForge.png를 실제로 측정해서 넣은 값이다(좌상단 원점 비율).
/// 배경 그림을 다시 그리면 이 값들도 다시 재야 한다.
/// </summary>
public static class WeaponForgeSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/WeaponForge.unity";

    private const string BackgroundPath = "Assets/UI/WeaponForge.png";
    private const string FontPath = "Assets/Fonts/Gaegu-Regular.ttf";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // ── 목업에서 측정한 영역 (xMin, yMin, xMax, yMax — 좌상단 원점) ──
    private static readonly Rect CanvasArea = Rect.MinMaxRect(0.1394f, 0.2136f, 0.6041f, 0.7811f);
    private static readonly Rect PreviewArea = Rect.MinMaxRect(0.6531f, 0.3252f, 0.8517f, 0.6727f);
    private static readonly Rect ForgeButtonArea = Rect.MinMaxRect(0.6364f, 0.7279f, 0.9115f, 0.9320f);
    private static readonly Rect BackButtonArea = Rect.MinMaxRect(0.0293f, 0.8842f, 0.0999f, 0.9681f);

    // 목업에 입력칸이 없어서, 미리보기와 "무기 만들기" 버튼 사이 빈 띠를 쓴다
    private static readonly Rect NoteArea = Rect.MinMaxRect(0.6364f, 0.6780f, 0.9115f, 0.7240f);
    private static readonly Rect StatusArea = Rect.MinMaxRect(0.1346f, 0.1250f, 0.6041f, 0.2050f);

    /// <summary>도구바 아이콘 9개의 가로 중심 (연필·크레용·지우개·undo·redo·검·빨·파·초)</summary>
    private static readonly float[] ToolCenters =
    {
        0.1719f, 0.2291f, 0.2904f, 0.3469f, 0.3956f, 0.4483f, 0.4913f, 0.5338f, 0.5763f
    };

    private const float ToolCenterY = 0.8582f;
    private const float ToolHalfWidth = 0.0215f;
    private const float ToolHalfHeight = 0.0430f;

    [MenuItem("Tools/NaManMoo/Build Weapon Forge")]
    public static void Build()
    {
        Sprite background = LoadBackgroundSprite();
        Font font = LoadFont();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        EnsureEventSystem();

        Canvas canvas = CreateCanvas();

        // 배경 그림과 상호작용 요소를 같은 16:9 프레임 안에 둔다.
        // 창 비율이 16:9가 아닐 때 배경만 레터박스되면 버튼 위치가 그림과 어긋난다.
        // (TitleSceneBuilder의 Title Frame과 같은 방식)
        RectTransform frame = CreateFrame(canvas.transform, background);
        CreateBackground(frame, background);

        var controllerObject = new GameObject(
            "Weapon Forge Controller",
            typeof(WeaponForgeController));
        WeaponForgeController controller =
            controllerObject.GetComponent<WeaponForgeController>();

        DrawingCanvas drawing = CreateDrawingCanvas(frame);
        RawImage preview = CreatePreview(frame);
        InputField note = CreateNoteInput(frame, font);
        Text status = CreateStatusText(frame, font);
        Button forgeButton = CreateInvisibleButton(frame, "Forge Button", ForgeButtonArea);
        Button backButton = CreateInvisibleButton(frame, "Back Button", BackButtonArea);

        CreateToolButtons(frame, controller);
        // 선택 화면은 그림에 얹는 게 아니라 화면 전체를 덮어야 하므로 캔버스 바로 아래에 둔다
        ChoicePanelParts choice = CreateChoicePanel(canvas.transform, font, controller);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            forgeButton.onClick, controller.Forge);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            backButton.onClick, controller.GoBackToTitle);

        WireController(controller, drawing, note, preview, forgeButton, status, choice);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Debug.Log($"[WeaponForgeSceneBuilder] {ScenePath} 생성 완료");
    }

    // ── 개별 조각 ──────────────────────────────────────────────

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.96f, 0.95f, 0.92f, 1f);
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject(
            "Weapon Forge Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        // 배경 그림이 16:9라 가로/세로 어느 쪽에도 치우치지 않게 절반씩 맞춘다
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    /// <summary>배경 그림의 가로세로비를 유지하는 프레임. 안의 모든 요소가 함께 움직인다.</summary>
    private static RectTransform CreateFrame(Transform parent, Sprite sprite)
    {
        var frameObject = new GameObject(
            "Forge Frame",
            typeof(RectTransform),
            typeof(AspectRatioFitter));
        frameObject.transform.SetParent(parent, false);

        RectTransform frame = (RectTransform)frameObject.transform;
        Stretch(frame);

        AspectRatioFitter fitter = frameObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        return frame;
    }

    private static void CreateBackground(Transform parent, Sprite sprite)
    {
        var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)backgroundObject.transform;
        Stretch(rect);

        Image image = backgroundObject.GetComponent<Image>();
        image.sprite = sprite;
        // 프레임이 이미 비율을 잡아 주므로 여기서 또 맞출 필요가 없다
        image.preserveAspect = false;
        // 배경은 클릭을 먹지 않아야 캔버스·버튼이 정상 동작한다
        image.raycastTarget = false;
    }

    private static DrawingCanvas CreateDrawingCanvas(Transform parent)
    {
        // 목업의 캔버스 자리에 그려진 예시 무기를 가리는 흰 바탕.
        // 그리기 텍스처 자체는 투명이라야 내보낸 PNG에 알파가 남는다.
        var backing = new GameObject("Drawing Backing", typeof(RectTransform), typeof(Image));
        backing.transform.SetParent(parent, false);
        SetArea((RectTransform)backing.transform, CanvasArea);
        Image backingImage = backing.GetComponent<Image>();
        backingImage.color = Color.white;
        backingImage.raycastTarget = false;

        var canvasObject = new GameObject(
            "Drawing Canvas",
            typeof(RectTransform),
            typeof(RawImage),
            typeof(DrawingCanvas));
        canvasObject.transform.SetParent(parent, false);
        SetArea((RectTransform)canvasObject.transform, CanvasArea);

        return canvasObject.GetComponent<DrawingCanvas>();
    }

    private static RawImage CreatePreview(Transform parent)
    {
        var backing = new GameObject("Preview Backing", typeof(RectTransform), typeof(Image));
        backing.transform.SetParent(parent, false);
        SetArea((RectTransform)backing.transform, PreviewArea);
        Image backingImage = backing.GetComponent<Image>();
        backingImage.color = Color.white;
        backingImage.raycastTarget = false;

        var previewObject = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
        previewObject.transform.SetParent(parent, false);
        SetArea((RectTransform)previewObject.transform, PreviewArea);

        RawImage preview = previewObject.GetComponent<RawImage>();
        preview.raycastTarget = false;
        return preview;
    }

    private static InputField CreateNoteInput(Transform parent, Font font)
    {
        var inputObject = new GameObject(
            "Note Input",
            typeof(RectTransform),
            typeof(Image),
            typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        SetArea((RectTransform)inputObject.transform, NoteArea);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.9f);

        var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(inputObject.transform, false);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect);
        viewportRect.offsetMin = new Vector2(12f, 6f);
        viewportRect.offsetMax = new Vector2(-12f, -6f);

        Text placeholder = CreateText(
            viewport.transform, "Placeholder", font, "무기 설명 (예: 불이 나오는 빠른 검)");
        placeholder.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.alignment = TextAnchor.MiddleLeft;

        Text text = CreateText(viewport.transform, "Text", font, string.Empty);
        text.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 200;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static Text CreateStatusText(Transform parent, Font font)
    {
        Text status = CreateText(parent, "Status Text", font, string.Empty);
        SetArea((RectTransform)status.transform, StatusArea);
        status.color = new Color(0.75f, 0.15f, 0.1f, 1f);
        status.fontSize = 30;
        status.alignment = TextAnchor.UpperLeft;
        return status;
    }

    private static void CreateToolButtons(Transform parent, WeaponForgeController controller)
    {
        (string name, UnityEngine.Events.UnityAction action)[] tools =
        {
            ("Pen", controller.SelectPen),
            ("Crayon", controller.SelectCrayon),
            ("Eraser", controller.SelectEraser),
            ("Undo", controller.Undo),
            ("Redo", controller.Redo),
            ("Color Black", controller.SelectBlack),
            ("Color Red", controller.SelectRed),
            ("Color Blue", controller.SelectBlue),
            ("Color Green", controller.SelectGreen)
        };

        for (int index = 0; index < tools.Length; index++)
        {
            float centerX = ToolCenters[index];
            Rect area = Rect.MinMaxRect(
                centerX - ToolHalfWidth,
                ToolCenterY - ToolHalfHeight,
                centerX + ToolHalfWidth,
                ToolCenterY + ToolHalfHeight);

            Button button = CreateInvisibleButton(parent, tools[index].name + " Button", area);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick, tools[index].action);
        }
    }

    private struct ChoicePanelParts
    {
        public GameObject Panel;
        public RawImage[] Images;
        public Button[] Buttons;
        public Text[] Labels;
        public Text Headline;
    }

    private static ChoicePanelParts CreateChoicePanel(
        Transform parent,
        Font font,
        WeaponForgeController controller)
    {
        var panel = new GameObject("Choice Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Stretch((RectTransform)panel.transform);
        Image dim = panel.GetComponent<Image>();
        dim.color = new Color(0.08f, 0.08f, 0.1f, 0.94f);

        Text headline = CreateText(
            panel.transform, "Headline", font, "어떤 걸로 할까?");
        SetArea((RectTransform)headline.transform, Rect.MinMaxRect(0.1f, 0.08f, 0.9f, 0.18f));
        headline.color = Color.white;
        headline.fontSize = 56;
        headline.alignment = TextAnchor.MiddleCenter;

        var parts = new ChoicePanelParts
        {
            Panel = panel,
            Images = new RawImage[3],
            Buttons = new Button[3],
            Labels = new Text[3],
            Headline = headline
        };

        for (int index = 0; index < 3; index++)
        {
            float left = 0.06f + index * 0.315f;
            Rect cardArea = Rect.MinMaxRect(left, 0.22f, left + 0.27f, 0.74f);

            var card = new GameObject($"Choice {index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(panel.transform, false);
            SetArea((RectTransform)card.transform, cardArea);
            card.GetComponent<Image>().color = Color.white;

            var image = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(card.transform, false);
            RectTransform imageRect = (RectTransform)image.transform;
            Stretch(imageRect);
            imageRect.offsetMin = new Vector2(14f, 60f);
            imageRect.offsetMax = new Vector2(-14f, -14f);
            RawImage raw = image.GetComponent<RawImage>();
            raw.raycastTarget = false;

            Text label = CreateText(card.transform, "Label", font, $"{index + 1}.");
            SetArea((RectTransform)label.transform, Rect.MinMaxRect(0f, 0.86f, 1f, 1f));
            label.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            label.fontSize = 34;
            label.alignment = TextAnchor.MiddleCenter;

            Button button = card.GetComponent<Button>();
            int captured = index;
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                button.onClick, controller.ChooseVariant, captured);

            parts.Images[index] = raw;
            parts.Buttons[index] = button;
            parts.Labels[index] = label;
        }

        Button retry = CreateInvisibleButton(
            panel.transform, "Draw Again Button", Rect.MinMaxRect(0.38f, 0.79f, 0.62f, 0.9f));
        Text retryLabel = CreateText(retry.transform, "Label", font, "다시 그리기");
        Stretch((RectTransform)retryLabel.transform);
        retryLabel.color = Color.white;
        retryLabel.fontSize = 34;
        retryLabel.alignment = TextAnchor.MiddleCenter;
        retryLabel.raycastTarget = false;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            retry.onClick, controller.BackToDrawing);

        panel.SetActive(false);
        return parts;
    }

    private static void WireController(
        WeaponForgeController controller,
        DrawingCanvas drawing,
        InputField note,
        RawImage preview,
        Button forgeButton,
        Text status,
        ChoicePanelParts choice)
    {
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("drawingCanvas").objectReferenceValue = drawing;
        serialized.FindProperty("noteInput").objectReferenceValue = note;
        serialized.FindProperty("previewImage").objectReferenceValue = preview;
        serialized.FindProperty("forgeButton").objectReferenceValue = forgeButton;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("choicePanel").objectReferenceValue = choice.Panel;
        serialized.FindProperty("choiceHeadline").objectReferenceValue = choice.Headline;

        AssignArray(serialized, "choiceImages", choice.Images);
        AssignArray(serialized, "choiceButtons", choice.Buttons);
        AssignArray(serialized, "choiceLabels", choice.Labels);

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

    // ── 공용 도우미 ─────────────────────────────────────────────

    private static Button CreateInvisibleButton(Transform parent, string name, Rect area)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetArea((RectTransform)buttonObject.transform, area);

        Image image = buttonObject.GetComponent<Image>();
        // 배경 그림에 이미 버튼이 그려져 있으므로, 눌리는 판만 얹는다
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
        colors.pressedColor = new Color(0f, 0f, 0f, 0.14f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;
        return button;
    }

    private static Text CreateText(
        Transform parent, string name, Font font, string content)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Stretch((RectTransform)textObject.transform);

        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = 32;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        // 상태 메시지가 길어져도 잘리지 않게
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

    private static Sprite LoadBackgroundSprite()
    {
        var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException(
                $"무기 만들기 배경 그림이 없습니다: {BackgroundPath}");
        }

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.wrapMode != TextureWrapMode.Clamp ||
            importer.mipmapEnabled ||
            !importer.alphaIsTransparency ||
            importer.maxTextureSize < 2048)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException(
                $"{BackgroundPath}를 Sprite로 읽지 못했습니다.");
        }

        return sprite;
    }

    /// <summary>
    /// 한글 표시용 폰트. WebGL에서는 OS 폰트(Arial 폴백)를 쓸 수 없어 한글이 깨지므로,
    /// 프로젝트에 넣은 Gaegu(OFL)를 직접 참조한다. 프로젝트 안의 TTF는
    /// 빌드에 포함되고 런타임에 동적으로 래스터화되므로 WebGL에서도 동작한다.
    /// </summary>
    private static Font LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            throw new System.InvalidOperationException(
                $"폰트가 없습니다: {FontPath}\n" +
                "AIGame의 web/public/fonts/Gaegu-Regular.ttf를 복사해 두세요.");
        }

        return font;
    }

    /// <summary>
    /// 빌드 씬 목록에 무기 만들기 씬을 끼워 넣는다. WebGL 빌더가 이 목록을 그대로
    /// 쓰므로 여기 없으면 브라우저에서 화면 전환이 실패한다.
    /// </summary>
    private static void AddSceneToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        if (scenes.Exists(scene => scene.path == ScenePath))
        {
            return;
        }

        // Title 바로 뒤 — 흐름 순서와 목록 순서를 맞춘다
        int titleIndex = scenes.FindIndex(
            scene => scene.path == TitleSceneBuilder.ScenePath);
        scenes.Insert(titleIndex + 1, new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
