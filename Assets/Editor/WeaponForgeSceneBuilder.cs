using System.Collections.Generic;
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
///
/// 목업에 없는 것(단계 슬라이더, 확장 팔레트, 상태 문구)은 그림의 빈 여백에 얹는다.
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

    // 제목과 캔버스 사이 빈 띠 — 단계 슬라이더.
    // 배경 그림을 재보면 제목 잉크는 y=0.165에서 끝나고 캔버스 테두리는 0.203에서
    // 시작한다. 그 사이 38/1000이 전부라, 라벨과 슬라이더를 위아래로 쌓지 못하고
    // 가로로 나란히 놓는다.
    private static readonly Rect StageLabelArea = Rect.MinMaxRect(0.1394f, 0.1680f, 0.3620f, 0.2010f);
    private static readonly Rect StageSliderArea = Rect.MinMaxRect(0.3700f, 0.1720f, 0.6041f, 0.1970f);

    // 도구바 아래 빈 여백 — 확장 팔레트
    private const float PaletteTop = 0.9250f;
    private const float PaletteBottom = 0.9760f;
    private const float PaletteLeft = 0.1400f;
    private const float PaletteRight = 0.6041f;

    // 오른쪽 아래 빈 여백 — 상태 문구
    private static readonly Rect StatusArea = Rect.MinMaxRect(0.6200f, 0.9380f, 0.9960f, 0.9960f);

    /// <summary>도구바 아이콘 9개의 가로 중심 (연필·크레용·지우개·undo·redo·검·빨·파·초)</summary>
    private static readonly float[] ToolCenters =
    {
        0.1719f, 0.2291f, 0.2904f, 0.3469f, 0.3956f, 0.4483f, 0.4913f, 0.5338f, 0.5763f
    };

    private const float ToolCenterY = 0.8582f;
    private const float ToolHalfWidth = 0.0215f;
    private const float ToolHalfHeight = 0.0430f;

    // 선택 표시는 아이콘 그림을 덮지 않도록 밑줄로 — 도구바 안쪽 아래에 놓는다
    private const float ToolUnderlineTop = 0.9010f;
    private const float ToolUnderlineBottom = 0.9105f;

    private static readonly Color HighlightColor = new Color(0.98f, 0.55f, 0.1f, 1f);

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

        (Slider stageSlider, Text stageLabel) = CreateStageSlider(frame, font);
        Image[] toolHighlights = CreateToolButtons(frame, controller);
        Image[] colorHighlights = CreateColorButtons(frame, controller);
        // 결과 화면은 그림에 얹는 게 아니라 화면 전체를 덮으므로 캔버스 바로 아래에 둔다
        ResultPanelParts result = CreateResultPanel(canvas.transform, font, controller);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            forgeButton.onClick, controller.Forge);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            backButton.onClick, controller.GoBackToTitle);

        WireController(
            controller, drawing, note, preview, forgeButton, status,
            stageSlider, stageLabel, toolHighlights, colorHighlights, result);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Debug.Log($"[WeaponForgeSceneBuilder] {ScenePath} 생성 완료");
    }

    // ── 화면 골격 ──────────────────────────────────────────────

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
        Stretch((RectTransform)backgroundObject.transform);

        Image image = backgroundObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        // 배경은 클릭을 먹지 않아야 캔버스·버튼이 정상 동작한다
        image.raycastTarget = false;
    }

    // ── 그리기·미리보기 ─────────────────────────────────────────

    private static DrawingCanvas CreateDrawingCanvas(Transform parent)
    {
        // 목업의 캔버스 자리에 그려진 예시 무기를 가리는 흰 바탕.
        // 그리기 텍스처 자체는 투명이라야 내보낸 PNG에 알파가 남는다.
        CreateSolid(parent, "Drawing Backing", CanvasArea, Color.white);

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
        CreateSolid(parent, "Preview Backing", PreviewArea, Color.white);

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
            "Note Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        SetArea((RectTransform)inputObject.transform, NoteArea);
        inputObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

        var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(inputObject.transform, false);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect);
        viewportRect.offsetMin = new Vector2(12f, 6f);
        viewportRect.offsetMax = new Vector2(-12f, -6f);

        Text placeholder = CreateText(
            viewport.transform, "Placeholder", font, "무기 설명 (예: 불이 나오는 빠른 검)", 28);
        placeholder.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.alignment = TextAnchor.MiddleLeft;

        Text text = CreateText(viewport.transform, "Text", font, string.Empty, 28);
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
        Text status = CreateText(parent, "Status Text", font, string.Empty, 24);
        SetArea((RectTransform)status.transform, StatusArea);
        status.color = new Color(0.72f, 0.14f, 0.09f, 1f);
        status.alignment = TextAnchor.UpperLeft;
        return status;
    }

    // ── 단계 슬라이더 ───────────────────────────────────────────

    /// <summary>
    /// AI 개입 단계 0/1/2를 고르는 슬라이더. 목업에 없는 요소라 제목과 캔버스 사이
    /// 빈 띠에 얹는다. 칸이 세 개뿐이므로 wholeNumbers로 고정한다.
    /// </summary>
    private static (Slider, Text) CreateStageSlider(Transform parent, Font font)
    {
        Text label = CreateText(parent, "Stage Label", font, string.Empty, 30);
        SetArea((RectTransform)label.transform, StageLabelArea);
        label.color = new Color(0.16f, 0.16f, 0.18f, 1f);
        label.alignment = TextAnchor.MiddleLeft;

        var sliderObject = new GameObject("Stage Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        SetArea((RectTransform)sliderObject.transform, StageSliderArea);

        Image background = CreateSolid(
            sliderObject.transform, "Background", null, new Color(0.80f, 0.78f, 0.74f, 1f));
        SetVerticalBand((RectTransform)background.transform, 0.30f, 0.70f);
        // 트랙도 클릭을 받아야 한다. 끄면 손잡이를 정확히 잡아 끌어야만 값이 바뀐다.
        background.raycastTarget = true;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        SetVerticalBand((RectTransform)fillArea.transform, 0.30f, 0.70f);
        Image fill = CreateSolid(fillArea.transform, "Fill", null, HighlightColor);
        Stretch((RectTransform)fill.transform);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        Stretch((RectTransform)handleArea.transform);
        Image handle = CreateSolid(
            handleArea.transform, "Handle", null, new Color(0.15f, 0.15f, 0.17f, 1f));
        handle.raycastTarget = true;
        RectTransform handleRect = (RectTransform)handle.transform;
        handleRect.sizeDelta = new Vector2(28f, 34f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = (RectTransform)fill.transform;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = WeaponForgeController.MaxStage;
        slider.wholeNumbers = true;
        slider.value = 0f;
        return (slider, label);
    }

    // ── 도구·색 버튼 ───────────────────────────────────────────

    /// <summary>연필·크레용·지우개·undo·redo. 앞 3개만 선택 표시를 갖는다.</summary>
    private static Image[] CreateToolButtons(Transform parent, WeaponForgeController controller)
    {
        string[] names = { "Pen", "Crayon", "Eraser" };
        var highlights = new Image[names.Length];

        for (int index = 0; index < names.Length; index++)
        {
            Button button = CreateInvisibleButton(
                parent, names[index] + " Button", ToolIconArea(index));
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                button.onClick, controller.SelectTool, index);
            highlights[index] = CreateUnderline(parent, names[index] + " Selected", index);
        }

        Button undo = CreateInvisibleButton(parent, "Undo Button", ToolIconArea(3));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(undo.onClick, controller.Undo);

        Button redo = CreateInvisibleButton(parent, "Redo Button", ToolIconArea(4));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(redo.onClick, controller.Redo);

        return highlights;
    }

    /// <summary>
    /// 색 버튼. 앞 4개는 목업 도구바에 그려진 점 위에 투명 버튼을 얹고,
    /// 나머지는 도구바 아래 여백에 색 조각을 직접 그린다.
    /// </summary>
    private static Image[] CreateColorButtons(Transform parent, WeaponForgeController controller)
    {
        Color32[] colors = WeaponForgeController.PaletteColors;
        int extendedStart = WeaponForgeController.ExtendedPaletteStart;
        var highlights = new Image[colors.Length];

        // 목업에 그려진 4색 — 도구바의 6~9번째 아이콘 자리
        for (int index = 0; index < extendedStart; index++)
        {
            int iconSlot = 5 + index;
            Button button = CreateInvisibleButton(
                parent, $"Color {index} Button", ToolIconArea(iconSlot));
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                button.onClick, controller.SelectColor, index);
            highlights[index] = CreateUnderline(parent, $"Color {index} Selected", iconSlot);
        }

        // 확장 팔레트 — 목업에 없으므로 조각과 밑줄을 전부 그린다
        int extendedCount = colors.Length - extendedStart;
        float slotWidth = (PaletteRight - PaletteLeft) / extendedCount;
        float gap = slotWidth * 0.18f;

        for (int offset = 0; offset < extendedCount; offset++)
        {
            int index = extendedStart + offset;
            float left = PaletteLeft + offset * slotWidth;
            Rect swatchArea = Rect.MinMaxRect(
                left + gap * 0.5f, PaletteTop, left + slotWidth - gap * 0.5f, PaletteBottom);

            // 흰색·밝은 조각은 종이 위에서 경계가 안 보이므로 테두리를 먼저 깔아 준다
            Color32 color = colors[index];
            if (color.r > 200 && color.g > 200 && color.b > 200)
            {
                CreateSolid(
                    parent, $"Color {index} Border",
                    Grow(swatchArea, 0.0020f, 0.0034f),
                    new Color(0.45f, 0.45f, 0.45f, 1f));
            }

            var swatchObject = new GameObject(
                $"Color {index} Swatch", typeof(RectTransform), typeof(Image), typeof(Button));
            swatchObject.transform.SetParent(parent, false);
            SetArea((RectTransform)swatchObject.transform, swatchArea);

            Image swatch = swatchObject.GetComponent<Image>();
            swatch.color = color;

            Button button = swatchObject.GetComponent<Button>();
            button.targetGraphic = swatch;
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                button.onClick, controller.SelectColor, index);

            Rect underlineArea = Rect.MinMaxRect(
                swatchArea.xMin, PaletteBottom + 0.005f,
                swatchArea.xMax, PaletteBottom + 0.016f);
            Image underline = CreateSolid(
                parent, $"Color {index} Selected", underlineArea, HighlightColor);
            underline.enabled = false;
            highlights[index] = underline;
        }

        return highlights;
    }

    private static Rect ToolIconArea(int slot)
    {
        float centerX = ToolCenters[slot];
        return Rect.MinMaxRect(
            centerX - ToolHalfWidth,
            ToolCenterY - ToolHalfHeight,
            centerX + ToolHalfWidth,
            ToolCenterY + ToolHalfHeight);
    }

    /// <summary>도구바 아이콘 아래에 그리는 선택 표시. 아이콘 그림을 덮지 않는다.</summary>
    private static Image CreateUnderline(Transform parent, string name, int slot)
    {
        float centerX = ToolCenters[slot];
        Rect area = Rect.MinMaxRect(
            centerX - ToolHalfWidth * 0.72f,
            ToolUnderlineTop,
            centerX + ToolHalfWidth * 0.72f,
            ToolUnderlineBottom);

        Image underline = CreateSolid(parent, name, area, HighlightColor);
        underline.enabled = false;
        return underline;
    }

    // ── 결과 확인 화면 ──────────────────────────────────────────

    private struct ResultPanelParts
    {
        public GameObject Panel;
        public RawImage Image;
        public Text Headline;
        public Text Detail;
    }

    private static ResultPanelParts CreateResultPanel(
        Transform parent, Font font, WeaponForgeController controller)
    {
        var panel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Stretch((RectTransform)panel.transform);
        // 프로젝트가 Linear 색공간이라 알파를 sRGB 감각으로 잡으면 훨씬 밝게 나온다.
        // 0.94로는 배경이 (72,72,74)까지밖에 안 어두워져 뒷그림이 내용과 경합했다.
        panel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.98f);

        Text headline = CreateText(panel.transform, "Headline", font, string.Empty, 58);
        SetArea((RectTransform)headline.transform, Rect.MinMaxRect(0.1f, 0.06f, 0.9f, 0.16f));
        headline.color = Color.white;
        headline.alignment = TextAnchor.MiddleCenter;

        Rect card = Rect.MinMaxRect(0.355f, 0.19f, 0.645f, 0.62f);
        CreateSolid(panel.transform, "Result Card", card, Color.white);

        var imageObject = new GameObject("Result Image", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(panel.transform, false);
        SetArea((RectTransform)imageObject.transform, Grow(card, -0.008f, -0.014f));
        imageObject.GetComponent<RawImage>().raycastTarget = false;

        Text detail = CreateText(panel.transform, "Detail", font, string.Empty, 30);
        SetArea((RectTransform)detail.transform, Rect.MinMaxRect(0.12f, 0.64f, 0.88f, 0.71f));
        detail.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        detail.alignment = TextAnchor.MiddleCenter;

        Button confirm = CreateLabeledButton(
            panel.transform, font, "Confirm Button", "이걸로 하기",
            Rect.MinMaxRect(0.30f, 0.75f, 0.485f, 0.84f),
            new Color(0.16f, 0.62f, 0.36f, 1f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            confirm.onClick, controller.ConfirmResult);

        Button retry = CreateLabeledButton(
            panel.transform, font, "Retry Button", "다시 그리기",
            Rect.MinMaxRect(0.515f, 0.75f, 0.70f, 0.84f),
            new Color(0.35f, 0.35f, 0.40f, 1f));
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            retry.onClick, controller.BackToDrawing);

        panel.SetActive(false);
        return new ResultPanelParts
        {
            Panel = panel,
            Image = imageObject.GetComponent<RawImage>(),
            Headline = headline,
            Detail = detail
        };
    }

    // ── 배선 ──────────────────────────────────────────────────

    private static void WireController(
        WeaponForgeController controller,
        DrawingCanvas drawing,
        InputField note,
        RawImage preview,
        Button forgeButton,
        Text status,
        Slider stageSlider,
        Text stageLabel,
        Image[] toolHighlights,
        Image[] colorHighlights,
        ResultPanelParts result)
    {
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("drawingCanvas").objectReferenceValue = drawing;
        serialized.FindProperty("noteInput").objectReferenceValue = note;
        serialized.FindProperty("previewImage").objectReferenceValue = preview;
        serialized.FindProperty("forgeButton").objectReferenceValue = forgeButton;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("stageSlider").objectReferenceValue = stageSlider;
        serialized.FindProperty("stageLabel").objectReferenceValue = stageLabel;
        serialized.FindProperty("resultPanel").objectReferenceValue = result.Panel;
        serialized.FindProperty("resultImage").objectReferenceValue = result.Image;
        serialized.FindProperty("resultHeadline").objectReferenceValue = result.Headline;
        serialized.FindProperty("resultDetail").objectReferenceValue = result.Detail;

        AssignArray(serialized, "toolHighlights", toolHighlights);
        AssignArray(serialized, "colorHighlights", colorHighlights);

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

    private static Button CreateLabeledButton(
        Transform parent, Font font, string name, string label, Rect area, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetArea((RectTransform)buttonObject.transform, area);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText(buttonObject.transform, "Label", font, label, 34);
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
        // 문구가 길어져도 잘리지 않게
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

    /// <summary>부모 높이의 일부만 차지하는 가로 띠 (슬라이더 홈·채움에 쓴다).</summary>
    private static void SetVerticalBand(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
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

    private static Rect Grow(Rect area, float x, float y)
    {
        return Rect.MinMaxRect(area.xMin - x, area.yMin - y, area.xMax + x, area.yMax + y);
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
    /// 프로젝트에 넣은 Gaegu(OFL)를 직접 참조한다.
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
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == ScenePath))
        {
            return;
        }

        // Title 바로 뒤 — 흐름 순서와 목록 순서를 맞춘다
        int titleIndex = scenes.FindIndex(scene => scene.path == TitleSceneBuilder.ScenePath);
        scenes.Insert(titleIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
