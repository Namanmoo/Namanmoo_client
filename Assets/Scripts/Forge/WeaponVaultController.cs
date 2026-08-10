using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 무기고 화면. 서버에 저장된 무기를 카드로 늘어놓고 장착하거나 지운다.
///
/// 카드 칸은 씬 빌더가 미리 만들어 두고, 여기서 내용만 채운다 — 런타임에
/// UI를 새로 만들면 WebGL에서 첫 표시가 눈에 띄게 늦어진다.
/// </summary>
public sealed class WeaponVaultController : MonoBehaviour
{
    public const string ForgeScenePath = GameScenes.WeaponForge;

    /// <summary>장착하면 들어가는 곳. 무기 만들기 화면과 같은 곳으로 간다.</summary>
    public const string PlayScenePath = GameScenes.Dungeon;

    /// <summary>한 화면에 보여 줄 카드 수. 넘치는 건 최신순으로 잘린다.</summary>
    public const int CardCount = 8;

    [SerializeField] private string backendBaseUrl = ForgeClient.DefaultBaseUrl;
    [SerializeField] private Text statusText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button forgeButton;

    [Header("카드 (씬 빌더가 채운다)")]
    [SerializeField] private GameObject[] cards = new GameObject[CardCount];
    [SerializeField] private RawImage[] cardImages = new RawImage[CardCount];
    [SerializeField] private Text[] cardNames = new Text[CardCount];
    [SerializeField] private Text[] cardDetails = new Text[CardCount];
    [SerializeField] private Button[] equipButtons = new Button[CardCount];
    [SerializeField] private Button[] deleteButtons = new Button[CardCount];

    [Header("기준점 수정 (씬 빌더가 채운다)")]
    [SerializeField] private GameObject editPanel;
    [SerializeField] private RawImage editImage;
    [SerializeField] private WeaponPointPicker editPicker;
    [SerializeField] private Text editTitle;
    /// <summary>그립·중심·끝 마커 — <see cref="WeaponPointKind"/> 순서.</summary>
    [SerializeField] private RectTransform[] editMarkers = new RectTransform[3];
    /// <summary>어느 점을 옮기는 중인지 표시 — <see cref="WeaponPointKind"/> 순서.</summary>
    [SerializeField] private Image[] editPointHighlights = new Image[3];

    private static readonly string[] StageNames = { "그대로", "조금 멋있게", "완전 멋있게" };

    private readonly List<SavedWeaponDto> weapons = new();
    private readonly Dictionary<string, Texture2D> textures = new();
    private WeaponVaultClient client;
    private bool busy;

    // 기준점 수정 상태 — 저장 전까지는 화면에만 들고 있는다
    private int editIndex = -1;
    private WeaponPointKind editingPoint = WeaponPointKind.Grip;
    private readonly Vector2[] editPoints = new Vector2[3];

    /// <summary>지금 화면에 올라온 무기 수 — 테스트에서 확인하기 쉽게 열어 둔다.</summary>
    public int LoadedCount => weapons.Count;

    private void Awake()
    {
        client = new WeaponVaultClient(ForgeClient.ResolveBaseUrl(backendBaseUrl));
        HideAllCards();

        if (editPanel != null)
        {
            editPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (editPicker != null)
        {
            editPicker.Picked += MoveEditingPoint;
        }

        StartCoroutine(Refresh());
    }

    private void OnDestroy()
    {
        // 받아 둔 텍스처는 씬이 바뀔 때 저절로 사라지지 않는다
        foreach (Texture2D texture in textures.Values)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        textures.Clear();
    }

    // ── 버튼 ──────────────────────────────────────────────

    public void GoBackToForge()
    {
        if (!busy)
        {
            SceneManager.LoadScene(ForgeScenePath);
        }
    }

    /// <summary>"새로 만들기" — 무기 만들기 화면으로.</summary>
    public void GoToForge() => GoBackToForge();

    /// <summary>카드의 "장착" — 그 무기로 게임에 들어간다.</summary>
    public void Equip(int index)
    {
        if (busy || index < 0 || index >= weapons.Count)
        {
            return;
        }

        SavedWeaponDto weapon = weapons[index];
        if (!textures.TryGetValue(weapon.id, out Texture2D texture) || texture == null)
        {
            SetStatus("그림을 아직 받지 못했습니다. 잠시 후 다시 눌러 주세요.");
            return;
        }

        // 저장해 둔 그립이 곧 잡는 자리다 — 기준점 없이 저장된 옛 무기는 가운데.
        // 투명 여백은 잘려 나가서 스프라이트 경계 = 보이는 무기다.
        Sprite sprite = WeaponSpriteFactory.FromTexture(texture, weapon.name, weapon.Grip01);

        // 씬을 넘어가는 동안 텍스처가 지워지지 않게 소유권을 넘긴다
        textures.Remove(weapon.id);

        WeaponLoadout loadout = ForgeWeaponAssembler.FromDto(weapon.weapon, sprite, weapon.name);
        if (loadout.Definition != null)
        {
            // 그린 축(그립→끝)이 위에서 벗어난 만큼 손에 들 때 되돌린다
            loadout.Definition.SpriteAxisDegrees =
                WeaponDefinition.AxisDegrees(weapon.Grip01, weapon.Tip01);
        }

        ForgedWeapon.Set(sprite, loadout, weapon.weapon, weapon.name, weapon.stage);
        SceneManager.LoadScene(PlayScenePath);
    }

    // ── 기준점 수정 ────────────────────────────────────────

    /// <summary>카드의 "수정" — 그림 위에서 그립·중심·끝을 다시 찍는다.</summary>
    public void BeginEdit(int index)
    {
        if (busy || index < 0 || index >= weapons.Count || editPanel == null)
        {
            return;
        }

        SavedWeaponDto weapon = weapons[index];
        if (!textures.TryGetValue(weapon.id, out Texture2D texture) || texture == null)
        {
            SetStatus("그림을 아직 받지 못했습니다. 잠시 후 다시 눌러 주세요.");
            return;
        }

        editIndex = index;
        editPoints[(int)WeaponPointKind.Grip] = weapon.Grip01;
        editPoints[(int)WeaponPointKind.Center] = weapon.Center01;
        editPoints[(int)WeaponPointKind.Tip] = weapon.Tip01;

        if (editImage != null)
        {
            editImage.texture = texture;
        }

        if (editTitle != null)
        {
            editTitle.text = $"{weapon.name} — 기준점 수정";
        }

        editPanel.SetActive(true);
        SelectEditPoint((int)WeaponPointKind.Grip);
        RefreshEditMarkers();
    }

    /// <summary>수정 화면의 점 고르기 버튼 — <see cref="WeaponPointKind"/> 값.</summary>
    public void SelectEditPoint(int kind)
    {
        if (kind < 0 || kind >= editPoints.Length)
        {
            return;
        }

        editingPoint = (WeaponPointKind)kind;

        if (editPointHighlights == null)
        {
            return;
        }

        for (int index = 0; index < editPointHighlights.Length; index++)
        {
            if (editPointHighlights[index] != null)
            {
                editPointHighlights[index].enabled = index == kind;
            }
        }
    }

    /// <summary>수정 화면의 "저장" — 서버에 기준점만 고쳐 넣는다.</summary>
    public void SaveEdit()
    {
        if (busy || editIndex < 0 || editIndex >= weapons.Count)
        {
            return;
        }

        StartCoroutine(SaveEditRoutine());
    }

    /// <summary>수정 화면의 "취소".</summary>
    public void CancelEdit()
    {
        editIndex = -1;
        if (editPanel != null)
        {
            editPanel.SetActive(false);
        }
    }

    private void MoveEditingPoint(Vector2 normalized)
    {
        if (editIndex < 0)
        {
            return;
        }

        editPoints[(int)editingPoint] = normalized;
        RefreshEditMarkers();
    }

    /// <summary>마커를 그림 위 찍은 자리에 맞춘다 — 그림 영역 안에서 앵커만 움직인다.</summary>
    private void RefreshEditMarkers()
    {
        if (editMarkers == null)
        {
            return;
        }

        for (int index = 0; index < editMarkers.Length && index < editPoints.Length; index++)
        {
            RectTransform marker = editMarkers[index];
            if (marker == null)
            {
                continue;
            }

            marker.anchorMin = editPoints[index];
            marker.anchorMax = editPoints[index];
            marker.anchoredPosition = Vector2.zero;
        }
    }

    private IEnumerator SaveEditRoutine()
    {
        busy = true;
        SetStatus("기준점을 저장하는 중…");

        SavedWeaponDto weapon = weapons[editIndex];
        SavedWeaponDto updated = null;
        string failure = null;

        yield return client.UpdatePoints(
            weapon.id,
            editPoints[(int)WeaponPointKind.Grip],
            editPoints[(int)WeaponPointKind.Center],
            editPoints[(int)WeaponPointKind.Tip],
            result => updated = result,
            error => failure = error);

        busy = false;

        if (updated == null)
        {
            SetStatus(failure ?? "기준점을 저장하지 못했습니다.");
            yield break;
        }

        // 서버가 확정한 값으로 목록을 맞춘다 — 다음 장착부터 바로 쓰인다
        weapons[editIndex] = updated;
        SetStatus($"{updated.name} 기준점을 저장했습니다.");
        CancelEdit();
    }

    /// <summary>카드의 "삭제".</summary>
    public void Delete(int index)
    {
        if (busy || index < 0 || index >= weapons.Count)
        {
            return;
        }

        StartCoroutine(DeleteRoutine(weapons[index].id));
    }

    // ── 목록 갱신 ──────────────────────────────────────────

    private IEnumerator Refresh()
    {
        busy = true;
        SetStatus("무기고를 불러오는 중…");
        weapons.Clear();
        HideAllCards();

        SavedWeaponDto[] received = null;
        string failure = null;
        yield return client.List(result => received = result, error => failure = error);

        if (received == null)
        {
            busy = false;
            SetStatus(failure ?? "무기고를 불러오지 못했습니다.");
            yield break;
        }

        for (int index = 0; index < received.Length && index < CardCount; index++)
        {
            weapons.Add(received[index]);
        }

        ShowCards();
        SetStatus(
            weapons.Count == 0
                ? "무기고가 비어 있습니다. 무기를 만들어 보세요."
                : $"무기 {weapons.Count}개"
                    + (received.Length > CardCount
                        ? $" (최신 {CardCount}개만 표시)"
                        : string.Empty));

        busy = false;

        // 카드가 다 자리를 잡은 뒤 그림을 받는다 — 목록이 먼저 보이는 편이 낫다
        for (int index = 0; index < weapons.Count; index++)
        {
            yield return LoadCardImage(index);
        }
    }

    private IEnumerator LoadCardImage(int index)
    {
        SavedWeaponDto weapon = weapons[index];
        Texture2D received = null;
        string failure = null;

        yield return client.LoadImage(
            weapon.id, texture => received = texture, error => failure = error);

        if (received == null)
        {
            Debug.LogWarning($"[WeaponVault] {weapon.name} 그림을 받지 못했습니다 — {failure}");
            yield break;
        }

        textures[weapon.id] = received;
        if (index < cardImages.Length && cardImages[index] != null)
        {
            cardImages[index].texture = received;
            cardImages[index].color = Color.white;
        }
    }

    private IEnumerator DeleteRoutine(string weaponId)
    {
        busy = true;
        SetStatus("지우는 중…");

        string failure = null;
        yield return client.Delete(weaponId, () => { }, error => failure = error);

        if (failure != null)
        {
            busy = false;
            SetStatus(failure);
            yield break;
        }

        if (textures.TryGetValue(weaponId, out Texture2D texture) && texture != null)
        {
            Destroy(texture);
            textures.Remove(weaponId);
        }

        busy = false;
        yield return Refresh();
    }

    // ── 표시 ──────────────────────────────────────────────

    private void ShowCards()
    {
        for (int index = 0; index < cards.Length; index++)
        {
            bool filled = index < weapons.Count;
            if (cards[index] != null)
            {
                cards[index].SetActive(filled);
            }

            if (!filled)
            {
                continue;
            }

            SavedWeaponDto weapon = weapons[index];

            if (cardNames[index] != null)
            {
                cardNames[index].text = weapon.name;
            }

            if (cardDetails[index] != null)
            {
                WeaponLoadout loadout =
                    ForgeWeaponAssembler.FromDto(weapon.weapon, null, weapon.name);
                string stage = weapon.stage >= 0 && weapon.stage < StageNames.Length
                    ? StageNames[weapon.stage]
                    : weapon.stage.ToString();
                string effects = WeaponSummary.Effects(loadout);
                string detail = string.IsNullOrEmpty(effects)
                    ? $"{stage} · {WeaponSummary.Headline(loadout)}\n{WeaponSummary.Stats(loadout)}"
                    : $"{stage} · {WeaponSummary.Headline(loadout)}\n{WeaponSummary.Stats(loadout)}\n효과: {effects}";

                // 플레이버는 카드에서 유일하게 무기마다 다른 문장 — 맨 위에 둔다
                cardDetails[index].text = string.IsNullOrWhiteSpace(weapon.flavor)
                    ? detail
                    : weapon.flavor.Trim() + "\n" + detail;
            }

            if (cardImages[index] != null)
            {
                // 그림이 도착할 때까지 빈 칸으로 둔다
                cardImages[index].texture = null;
                cardImages[index].color = new Color(1f, 1f, 1f, 0.15f);
            }
        }
    }

    private void HideAllCards()
    {
        foreach (GameObject card in cards)
        {
            if (card != null)
            {
                card.SetActive(false);
            }
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
