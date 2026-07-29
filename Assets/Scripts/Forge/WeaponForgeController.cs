using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기 만들기 화면의 상태 기계.
///
/// 그리기 → (무기 만들기) → 생성 중 → 3버전 중 선택 → 확정 → Stage1.
/// 서버가 죽어 있거나 생성이 실패해도 흐름은 끊기지 않는다 — 실패한 버전은
/// 원본 그림으로 채우고, 최악의 경우 그린 그림 그대로 게임에 들어간다.
/// </summary>
public sealed class WeaponForgeController : MonoBehaviour
{
    public const string TitleScenePath = "Assets/Scenes/Title.unity";
    public const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";

    public enum Phase
    {
        Drawing,
        Forging,
        Choosing
    }

    [SerializeField] private string backendBaseUrl = ForgeClient.DefaultBaseUrl;
    [SerializeField] private DrawingCanvas drawingCanvas;
    [SerializeField] private InputField noteInput;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button forgeButton;
    [SerializeField] private Text statusText;

    [Header("3버전 선택")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private RawImage[] choiceImages = new RawImage[3];
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private Text[] choiceLabels = new Text[3];
    [SerializeField] private Text choiceHeadline;

    private static readonly string[] VersionNames = { "그대로", "다듬기", "완전 새로" };

    private ForgeResponseDto response;
    private readonly Sprite[] candidateSprites = new Sprite[3];
    private Coroutine running;

    public Phase Current { get; private set; } = Phase.Drawing;

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
        ShowChoicePanel(false);
        SetStatus(string.Empty);
    }

    private void Start()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.Changed += RefreshPreview;
            RefreshPreview();
        }
    }

    private void OnDestroy()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.Changed -= RefreshPreview;
        }
    }

    // ── 도구바 버튼이 직접 부르는 것들 ──────────────────────────────

    public void SelectPen() => drawingCanvas?.SetTool(BrushKind.Pen);

    public void SelectCrayon() => drawingCanvas?.SetTool(BrushKind.Crayon);

    public void SelectEraser() => drawingCanvas?.SetTool(BrushKind.Eraser);

    public void Undo() => drawingCanvas?.Undo();

    public void Redo() => drawingCanvas?.Redo();

    public void SelectBlack() => SetColor(new Color32(30, 30, 30, 255));

    public void SelectRed() => SetColor(new Color32(220, 40, 40, 255));

    public void SelectBlue() => SetColor(new Color32(40, 90, 220, 255));

    public void SelectGreen() => SetColor(new Color32(30, 165, 90, 255));

    private void SetColor(Color32 color) => drawingCanvas?.SetColor(color);

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

        running = StartCoroutine(ForgeRoutine());
    }

    private IEnumerator ForgeRoutine()
    {
        Current = Phase.Forging;
        SetInteractable(false);
        SetStatus("대장간에 그림을 보내는 중…");

        byte[] png = drawingCanvas.EncodeToPng();
        string note = noteInput != null ? noteInput.text : string.Empty;
        var client = new ForgeClient(backendBaseUrl);

        ForgeResponseDto received = null;
        string failure = null;

        yield return client.Post(
            png,
            note,
            result => received = result,
            error => failure = error);

        running = null;

        if (received == null)
        {
            // 서버에 못 닿아도 그린 그림으로는 놀 수 있어야 한다
            OfferDrawingOnlyFallback(png);
            SetStatus(failure);
            yield break;
        }

        response = received;
        BuildCandidates(png);
        ShowChoices();
    }

    /// <summary>
    /// 서버가 죽어 있을 때의 탈출구 — 1번(그대로)만 있는 선택지를 띄운다.
    /// 스탯은 기본 검과 같게 둔다.
    /// </summary>
    private void OfferDrawingOnlyFallback(byte[] png)
    {
        response = null;
        ClearCandidates();
        candidateSprites[0] = WeaponSpriteFactory.FromPng(png, false, "그린 무기");
        ShowChoices();
    }

    private void BuildCandidates(byte[] originalPng)
    {
        ClearCandidates();

        // 1번은 언제나 플레이어가 그린 원본 — 이미 투명 배경이라 키잉이 필요 없다
        candidateSprites[0] = WeaponSpriteFactory.FromPng(originalPng, false, "그린 무기");

        foreach (ForgeVariantDto variant in response.variants)
        {
            if (variant == null || variant.version < 2 || variant.version > 3)
            {
                continue;
            }

            // 생성 이미지는 흰 배경이 채워져 오므로 뚫어 준다
            candidateSprites[variant.version - 1] = WeaponSpriteFactory.FromBase64(
                variant.image,
                removeWhiteBackground: true,
                name: $"무기 {variant.version}번");
        }
    }

    private void ShowChoices()
    {
        Current = Phase.Choosing;
        ShowChoicePanel(true);

        string weaponName = response != null ? response.name : "그린 무기";
        if (choiceHeadline != null)
        {
            choiceHeadline.text = $"{weaponName} — 어떤 걸로 할까?";
        }

        for (int index = 0; index < choiceImages.Length; index++)
        {
            Sprite sprite = candidateSprites[index];
            bool available = sprite != null;

            if (choiceImages[index] != null)
            {
                choiceImages[index].texture = available ? sprite.texture : null;
                choiceImages[index].color = available
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.15f);
            }

            if (choiceButtons[index] != null)
            {
                choiceButtons[index].interactable = available;
            }

            if (choiceLabels[index] != null)
            {
                choiceLabels[index].text = available
                    ? $"{index + 1}. {VersionNames[index]}"
                    : $"{index + 1}. {VersionNames[index]} (실패)";
            }
        }

        bool anyGenerated = candidateSprites[1] != null || candidateSprites[2] != null;
        SetStatus(
            anyGenerated
                ? string.Empty
                : "생성된 그림이 없어 그린 그림만 쓸 수 있습니다.");
    }

    /// <summary>선택 버튼 3개가 각각 0/1/2로 부른다.</summary>
    public void ChooseVariant(int index)
    {
        if (Current != Phase.Choosing || index < 0 || index >= candidateSprites.Length)
        {
            return;
        }

        Sprite chosen = candidateSprites[index];
        if (chosen == null)
        {
            return;
        }

        WeaponStats stats = response != null
            ? WeaponStats.FromDto(response.stats)
            : WeaponStats.Default;
        string weaponName = response != null && !string.IsNullOrWhiteSpace(response.name)
            ? response.name
            : "그린 무기";

        ForgedWeapon.Set(chosen, stats, weaponName, index + 1);
        SceneManager.LoadScene(Stage1ScenePath);
    }

    /// <summary>선택을 취소하고 다시 그리기로 — 마음에 드는 게 없을 때.</summary>
    public void BackToDrawing()
    {
        if (Current != Phase.Choosing)
        {
            return;
        }

        ShowChoicePanel(false);
        ClearCandidates();
        Current = Phase.Drawing;
        SetInteractable(true);
        SetStatus(string.Empty);
    }

    // ── 표시 갱신 ──────────────────────────────────────────────

    private void RefreshPreview()
    {
        if (previewImage == null || drawingCanvas == null)
        {
            return;
        }

        previewImage.texture = drawingCanvas.Texture;
    }

    private void ShowChoicePanel(bool visible)
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(visible);
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
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    private void ClearCandidates()
    {
        for (int index = 0; index < candidateSprites.Length; index++)
        {
            candidateSprites[index] = null;
        }
    }
}
