using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기 만들기 화면의 상태 기계.
///
/// 그리기 → 단계 선택(슬라이더 0/1/2) → (무기 만들기) → 생성 중 → 결과 확인 → Stage1.
/// 고른 단계 하나만 생성하므로 이미지 API 호출은 최대 1회다(0단계는 0회).
///
/// 서버가 죽어 있거나 생성이 실패해도 흐름은 끊기지 않는다 — 그린 그림 그대로
/// 게임에 들어간다.
/// </summary>
public sealed class WeaponForgeController : MonoBehaviour
{
    public const string TitleScenePath = "Assets/Scenes/Title.unity";
    public const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";

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
    [SerializeField] private Image[] toolHighlights = new Image[3];
    [SerializeField] private Image[] colorHighlights;

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
            RefreshPreview();
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
        }
    }

    // ── 도구바·팔레트 버튼이 부르는 것들 ─────────────────────────────

    /// <summary>0=연필, 1=크레용, 2=지우개</summary>
    public void SelectTool(int index)
    {
        BrushKind kind = index switch
        {
            1 => BrushKind.Crayon,
            2 => BrushKind.Eraser,
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

        string generated = response != null ? response.image : null;
        if (!string.IsNullOrEmpty(generated))
        {
            // 생성 이미지는 흰 배경이 채워져 오므로 뚫어 준다
            resultSprite = WeaponSpriteFactory.FromBase64(
                generated, removeWhiteBackground: true, name: $"무기 {stage}단계");
        }

        if (resultSprite == null)
        {
            // 그린 원본은 이미 투명 배경이라 키잉이 필요 없다
            resultSprite = WeaponSpriteFactory.FromPng(originalPng, false, "그린 무기");
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
            WeaponStats stats = response != null
                ? WeaponStats.FromDto(response.stats)
                : WeaponStats.Default;
            resultDetail.text =
                $"공격력 {stats.Damage}   연사 {stats.ShotsPerSecond:0.##}/초   " +
                $"탄속 {stats.ProjectileSpeed:0.##}   사거리 {stats.Lifetime:0.##}초";
        }

        // 무엇이 어긋났는지는 숨기지 않고 알려 준다
        if (failure != null)
        {
            SetStatus(failure);
        }
        else if (response != null && response.imageFailed)
        {
            SetStatus($"{StageNames[stage]} 생성에 실패해 그린 그림을 그대로 씁니다.");
        }
        else
        {
            SetStatus(string.Empty);
        }
    }

    /// <summary>"이걸로 하기" — 무기를 확정하고 게임으로 들어간다.</summary>
    public void ConfirmResult()
    {
        if (Current != Phase.Confirming || resultSprite == null)
        {
            return;
        }

        WeaponStats stats = response != null
            ? WeaponStats.FromDto(response.stats)
            : WeaponStats.Default;
        string weaponName = response != null && !string.IsNullOrWhiteSpace(response.name)
            ? response.name
            : "그린 무기";

        ForgedWeapon.Set(resultSprite, stats, weaponName, Stage);
        SceneManager.LoadScene(Stage1ScenePath);
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
