using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기 만들기 화면의 상태 기계.
///
/// 그리기 → 단계 선택(슬라이더 0/1/2) → (무기 만들기) → 생성 중 → 결과 확인 → 던전.
/// 고른 단계 하나만 생성하므로 이미지 API 호출은 최대 1회다(0단계는 0회).
///
/// 서버가 죽어 있거나 생성이 실패해도 흐름은 끊기지 않는다 — 그린 그림 그대로
/// 게임에 들어간다.
/// </summary>
public sealed class WeaponForgeController : MonoBehaviour
{
    public const string TitleScenePath = GameScenes.Title;

    /// <summary>무기를 확정하면 들어가는 곳.</summary>
    public const string PlayScenePath = GameScenes.Dungeon;

    public const string VaultScenePath = GameScenes.WeaponVault;

    /// <summary>AI 개입 단계의 최댓값. 백엔드 MAX_STAGE와 같아야 한다.</summary>
    public const int MaxStage = 2;

    /// <summary>
    /// 고를 수 있는 색. 앞 4개는 목업 도구바에 그려진 검·빨·파·초와 같은 순서이고,
    /// 그 뒤는 확장 팔레트다(AIGame 그리기 도구와 같은 구성).
    /// 씬 빌더가 이 배열로 색 버튼을 만들므로 여기만 고치면 UI도 따라온다.
    /// </summary>
    public static readonly Color32[] PaletteColors =
    {
        // 목업 도구바의 4색
        new Color32(30, 30, 30, 255),
        new Color32(220, 40, 40, 255),
        new Color32(40, 90, 220, 255),
        new Color32(30, 165, 90, 255),
        // 확장 팔레트
        new Color32(255, 255, 255, 255),
        new Color32(230, 57, 70, 255),
        new Color32(255, 140, 66, 255),
        new Color32(255, 209, 102, 255),
        new Color32(138, 201, 38, 255),
        new Color32(42, 157, 143, 255),
        new Color32(33, 158, 188, 255),
        new Color32(90, 24, 154, 255),
        new Color32(255, 112, 166, 255),
        new Color32(141, 85, 36, 255),
        new Color32(173, 181, 189, 255),
        new Color32(90, 90, 90, 255)
    };

    /// <summary>확장 팔레트의 시작 인덱스 — 목업 도구바 4색 다음부터</summary>
    public const int ExtendedPaletteStart = 4;

    private static readonly string[] StageNames = { "그대로", "조금 멋있게", "완전 멋있게" };



    public enum Phase
    {
        Drawing,
        Forging,
        Confirming
    }

    [SerializeField] private string backendBaseUrl = ForgeClient.DefaultBaseUrl;
    [SerializeField] private DrawingCanvas drawingCanvas;
    [SerializeField] private InputField noteInput;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button forgeButton;
    [SerializeField] private Text statusText;

    [Header("AI 개입 단계")]
    [SerializeField] private Slider stageSlider;
    [SerializeField] private Text stageLabel;

    [Header("선택 상태 표시")]
    [SerializeField] private Image[] toolHighlights = new Image[7];
    [SerializeField] private Image[] colorHighlights;

    [Header("기준점 표시")]
    /// <summary>캔버스 위에 떠서 잡는 자리를 알려 주는 표시.</summary>
    [SerializeField] private RectTransform gripMarker;

    /// <summary>무기 몸통 가운데 표시.</summary>
    [SerializeField] private RectTransform centerMarker;

    /// <summary>칼끝 표시 — 손잡이→끝이 무기의 축이다.</summary>
    [SerializeField] private RectTransform tipMarker;

    [Header("결과 확인")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private RawImage resultImage;
    [SerializeField] private Text resultHeadline;
    [SerializeField] private Text resultDetail;

    private ForgeResponseDto response;
    private Sprite resultSprite;

    public Phase Current { get; private set; } = Phase.Drawing;

    /// <summary>슬라이더가 가리키는 단계 (0/1/2)</summary>
    public int Stage =>
        stageSlider != null ? Mathf.Clamp(Mathf.RoundToInt(stageSlider.value), 0, MaxStage) : 0;

    /// <summary>테스트에서 서버 주소를 바꿔 끼울 수 있게.</summary>
    public string BackendBaseUrl
    {
        get => backendBaseUrl;
        set => backendBaseUrl = value;
    }

    private void Awake()
    {
        // 화면에 들어올 때마다 이전 무기는 버린다 — 다시 만들러 온 것이므로
        ForgedWeapon.Clear();
        ShowResultPanel(false);
        SetStatus(string.Empty);
    }

    private void Start()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.Changed += RefreshPreview;
            drawingCanvas.PointChanged += RefreshPointMarker;
            RefreshPreview();
            RefreshPointMarker(WeaponPointKind.Grip);
            RefreshPointMarker(WeaponPointKind.Center);
            RefreshPointMarker(WeaponPointKind.Tip);
        }

        if (stageSlider != null)
        {
            stageSlider.onValueChanged.AddListener(_ => RefreshStageLabel());
        }

        RefreshStageLabel();
        // 기본 도구·색에 표시를 맞춰 둔다
        SelectTool(0);
        SelectColor(0);
    }

    private void OnDestroy()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.Changed -= RefreshPreview;
            drawingCanvas.PointChanged -= RefreshPointMarker;
        }
    }

    // ── 도구바·팔레트 버튼이 부르는 것들 ─────────────────────────────

    /// <summary>0=연필, 1=크레용, 2=지우개, 3=색 채우기, 4=그립, 5=중심, 6=끝</summary>
    public const int GripToolIndex = 4;

    /// <summary><see cref="GripToolIndex"/> 다음 — 무기 몸통 가운데를 찍는 도구.</summary>
    public const int CenterToolIndex = 5;

    /// <summary><see cref="CenterToolIndex"/> 다음 — 칼끝을 찍는 도구.</summary>
    public const int TipToolIndex = 6;

    /// <summary>0=연필, 1=크레용, 2=지우개, 3=색 채우기, 4=그립, 5=중심, 6=끝</summary>
    public void SelectTool(int index)
    {
        // 기준점은 칠하는 도구가 아니라서 붓 종류로 넘기지 않는다
        if (index >= GripToolIndex && index <= TipToolIndex)
        {
            drawingCanvas?.EnterPointMode(PointKindFor(index));
            Highlight(toolHighlights, index);
            return;
        }

        BrushKind kind = index switch
        {
            1 => BrushKind.Crayon,
            2 => BrushKind.Eraser,
            3 => BrushKind.Fill,
            _ => BrushKind.Pen
        };

        drawingCanvas?.SetTool(kind);
        Highlight(toolHighlights, index);
    }

    /// <summary><see cref="PaletteColors"/>의 인덱스</summary>
    public void SelectColor(int index)
    {
        if (index < 0 || index >= PaletteColors.Length)
        {
            return;
        }

        drawingCanvas?.SetColor(PaletteColors[index]);
        Highlight(colorHighlights, index);
        // 색을 고르면 지우개에서 빠져나오므로 도구 표시도 연필로 되돌린다
        if (drawingCanvas != null && drawingCanvas.Tool == BrushKind.Pen)
        {
            Highlight(toolHighlights, 0);
        }
    }

    public void Undo() => drawingCanvas?.Undo();

    public void Redo() => drawingCanvas?.Redo();

    // ── 화면 전환 ──────────────────────────────────────────────

    public void GoBackToTitle()
    {
        SceneManager.LoadScene(TitleScenePath);
    }

    /// <summary>"무기 만들기" 버튼.</summary>
    public void Forge()
    {
        if (Current != Phase.Drawing || drawingCanvas == null)
        {
            return;
        }

        if (drawingCanvas.IsEmpty())
        {
            SetStatus("먼저 무기를 그려 주세요.");
            return;
        }

        StartCoroutine(ForgeRoutine());
    }

    private IEnumerator ForgeRoutine()
    {
        int stage = Stage;
        Current = Phase.Forging;
        SetInteractable(false);
        SetStatus(stage == 0 ? "무기를 손질하는 중…" : "대장간에 그림을 보내는 중…");

        byte[] png = drawingCanvas.EncodeToPng();
        string note = noteInput != null ? noteInput.text : string.Empty;
        var client = new ForgeClient(backendBaseUrl);

        ForgeResponseDto received = null;
        string failure = null;

        yield return client.Post(
            png,
            note,
            stage,
            result => received = result,
            error => failure = error);

        response = received;
        BuildResult(png, stage);
        ShowResult(stage, failure);
    }

    /// <summary>
    /// 결과 스프라이트를 만든다. 생성 이미지가 없으면(0단계·실패·서버 불통)
    /// 그린 그림을 그대로 쓴다.
    /// </summary>
    private void BuildResult(byte[] originalPng, int stage)
    {
        resultSprite = null;

        // 그린 그림 기준으로 찍은 자리다. 생성 이미지도 같은 구도로 돌아오므로
        // 정규화 좌표는 그대로 통한다.
        Vector2 grip = drawingCanvas != null ? drawingCanvas.Grip : DrawingCanvas.DefaultGrip;

        string generated = response != null ? response.image : null;
        if (!string.IsNullOrEmpty(generated))
        {
            // 생성 이미지는 흰 배경이 채워져 오므로 뚫어 준다
            resultSprite = WeaponSpriteFactory.FromBase64(
                generated, removeWhiteBackground: true, name: $"무기 {stage}단계", pivot: grip);
        }

        if (resultSprite == null)
        {
            // 그린 원본은 이미 투명 배경이라 키잉이 필요 없다
            resultSprite = WeaponSpriteFactory.FromPng(originalPng, false, "그린 무기", grip);
        }
    }

    private void ShowResult(int stage, string failure)
    {
        Current = Phase.Confirming;
        ShowResultPanel(true);

        string weaponName = response != null && !string.IsNullOrWhiteSpace(response.name)
            ? response.name
            : "그린 무기";

        if (resultHeadline != null)
        {
            resultHeadline.text = weaponName;
        }

        if (resultImage != null)
        {
            resultImage.texture = resultSprite != null ? resultSprite.texture : null;
        }

        if (resultDetail != null)
        {
            string detail = WeaponSummary.Describe(BuildLoadout());
            string flavor = response != null ? response.flavor : null;
            resultDetail.text = string.IsNullOrWhiteSpace(flavor)
                ? detail
                : flavor.Trim() + "\n\n" + detail;
        }

        SetStatus(NoticeFor(stage, failure, response));
    }

    private string WeaponName()
    {
        return response != null && !string.IsNullOrWhiteSpace(response.name)
            ? response.name
            : "그린 무기";
    }

    /// <summary>
    /// 서버 응답을 게임이 쓰는 무기로 옮긴다. 응답이 없거나 해석에 실패하면
    /// 기본 원거리 무기를 쓴다 — 무기 없이 게임에 들어가는 일은 없어야 한다.
    /// </summary>
    private WeaponLoadout BuildLoadout()
    {
        return ForgeWeaponAssembler.FromDto(response?.weapon, resultSprite, WeaponName());
    }

    /// <summary>
    /// 결과에 붙일 안내 문구. 스탯과 그림은 따로 실패할 수 있어 둘 다 합쳐서 보여 준다.
    ///
    /// <c>fallback</c>을 빼먹으면 AI가 응답하지 못한 날에도 화면은 조용히 "연필 막대"만
    /// 띄운다. 플레이어는 자기 그림이 그렇게 해석된 줄로 안다 — 그래서 반드시 알린다.
    /// </summary>
    public static string NoticeFor(int stage, string failure, ForgeResponseDto result)
    {
        if (failure != null)
        {
            return failure;
        }

        if (result == null)
        {
            return string.Empty;
        }

        var notices = new List<string>();

        if (result.fallback)
        {
            notices.Add("AI가 응답하지 않아 기본 스탯이 들어갔습니다. 잠시 후 다시 만들어 보세요.");
        }

        if (result.imageFailed && stage > 0)
        {
            notices.Add($"{StageNames[Mathf.Clamp(stage, 0, StageNames.Length - 1)]} 생성에 실패해 그린 그림을 그대로 씁니다.");
        }

        return string.Join("  ", notices);
    }

    /// <summary>"이걸로 하기" — 무기를 확정하고 무기고에 넣은 뒤 게임으로 들어간다.</summary>
    public void ConfirmResult()
    {
        if (Current != Phase.Confirming || resultSprite == null)
        {
            return;
        }

        StartCoroutine(ConfirmRoutine());
    }

    private IEnumerator ConfirmRoutine()
    {
        string weaponName = WeaponName();
        string flavor = response != null ? response.flavor : string.Empty;
        WeaponLoadout loadout = BuildLoadout();

        // 그린 축(그립→끝)이 "위"에서 벗어난 만큼 손에 들 때 되돌린다
        Vector2 center = drawingCanvas != null
            ? drawingCanvas.WeaponCenter : DrawingCanvas.DefaultCenter;
        Vector2 tip = drawingCanvas != null ? drawingCanvas.Tip : DrawingCanvas.DefaultTip;
        if (loadout.Definition != null)
        {
            loadout.Definition.SpriteAxisDegrees = WeaponDefinition.AxisDegrees(
                drawingCanvas != null ? drawingCanvas.Grip : DrawingCanvas.DefaultGrip, tip);
        }

        ForgedWeapon.Set(resultSprite, loadout, response?.weapon, weaponName, Stage);

        // 무기고에 넣어 다음에도 꺼내 쓸 수 있게 한다.
        // 저장이 실패해도 이번 판은 그대로 진행한다 — 무기고 때문에 게임을 막지 않는다.
        SetStatus("무기고에 넣는 중…");
        var vault = new WeaponVaultClient(backendBaseUrl);
        string failure = null;

        // 그립은 캔버스에서 찍은 좌표 그대로 싣는다. 저장 PNG는 전체 그림인데
        // 스프라이트는 투명 여백이 잘려 있어서, 잘린 pivot을 역산해 보내면
        // 무기고에서 꺼낼 때 좌표계가 어긋난다.
        Vector2 grip = drawingCanvas != null ? drawingCanvas.Grip : DrawingCanvas.DefaultGrip;

        yield return vault.Save(
            resultSprite.texture.EncodeToPNG(),
            weaponName,
            flavor,
            Stage,
            ForgedWeapon.Source,
            grip,
            center,
            tip,
            _ => { },
            error => failure = error);

        if (failure != null)
        {
            Debug.LogWarning($"[WeaponForge] 무기고 저장 실패, 이번 판은 그대로 진행 — {failure}");
        }

        SceneManager.LoadScene(PlayScenePath);
    }

    /// <summary>"무기고" 버튼 — 저장된 무기를 꺼내 쓰는 화면으로.</summary>
    public void OpenVault()
    {
        if (Current == Phase.Forging)
        {
            return;  // 생성 중에는 화면을 떠나지 않는다
        }

        SceneManager.LoadScene(VaultScenePath);
    }

    /// <summary>"다시 그리기" — 결과가 마음에 안 들 때 그리기로 돌아간다.</summary>
    public void BackToDrawing()
    {
        if (Current != Phase.Confirming)
        {
            return;
        }

        ShowResultPanel(false);
        resultSprite = null;
        response = null;
        Current = Phase.Drawing;
        SetInteractable(true);
        SetStatus(string.Empty);
    }

    // ── 표시 갱신 ──────────────────────────────────────────────

    private void RefreshStageLabel()
    {
        if (stageLabel == null)
        {
            return;
        }

        int stage = Stage;
        // 배경 그림의 빈 띠가 좁아 문구를 짧게 유지한다
        stageLabel.text = $"AI {stage}단계 · {StageNames[stage]}";
    }

    private void RefreshPreview()
    {
        if (previewImage == null || drawingCanvas == null)
        {
            return;
        }

        previewImage.texture = drawingCanvas.Texture;
    }

    private static WeaponPointKind PointKindFor(int toolIndex) => toolIndex switch
    {
        CenterToolIndex => WeaponPointKind.Center,
        TipToolIndex => WeaponPointKind.Tip,
        _ => WeaponPointKind.Grip
    };

    /// <summary>
    /// 기준점 표시를 찍은 자리로 옮긴다. 캔버스와 같은 부모에 두고 앵커만 움직이므로
    /// 캔버스가 커지거나 창 비율이 바뀌어도 같은 자리를 가리킨다.
    /// </summary>
    private void RefreshPointMarker(WeaponPointKind kind)
    {
        RectTransform marker = kind switch
        {
            WeaponPointKind.Center => centerMarker,
            WeaponPointKind.Tip => tipMarker,
            _ => gripMarker
        };

        if (marker == null || drawingCanvas == null)
        {
            return;
        }

        var canvasRect = (RectTransform)drawingCanvas.transform;
        Vector2 point = drawingCanvas.Point(kind);
        Vector2 anchor = new Vector2(
            Mathf.Lerp(canvasRect.anchorMin.x, canvasRect.anchorMax.x, point.x),
            Mathf.Lerp(canvasRect.anchorMin.y, canvasRect.anchorMax.y, point.y));

        marker.anchorMin = anchor;
        marker.anchorMax = anchor;
        marker.anchoredPosition = Vector2.zero;
    }

    /// <summary>배열 중 하나만 보이게 한다 — 지금 고른 도구·색 표시.</summary>
    private static void Highlight(Image[] highlights, int selected)
    {
        if (highlights == null)
        {
            return;
        }

        for (int index = 0; index < highlights.Length; index++)
        {
            if (highlights[index] != null)
            {
                highlights[index].enabled = index == selected;
            }
        }
    }

    private void ShowResultPanel(bool visible)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(visible);
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (forgeButton != null)
        {
            forgeButton.interactable = interactable;
        }

        if (drawingCanvas != null)
        {
            drawingCanvas.enabled = interactable;
        }

        if (stageSlider != null)
        {
            stageSlider.interactable = interactable;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }
}
