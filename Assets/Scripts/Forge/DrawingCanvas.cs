using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 그림 위에 찍는 무기 기준점. 손잡이는 잡는 자리(스프라이트 pivot),
/// 끝은 칼끝 — 손잡이→끝이 무기의 축이 된다. 중심은 무기 몸통의 가운데다.
/// </summary>
public enum WeaponPointKind
{
    Grip,
    Center,
    Tip
}

/// <summary>
/// 그리기 캔버스. Texture2D에 직접 칠하고 RawImage로 보여 준다.
///
/// 텍스처는 <em>투명 배경</em>으로 시작한다. 목업의 캔버스 자리를 가리는 흰 바탕은
/// 뒤에 별도 Image로 깔린다(WeaponForgeSceneBuilder). 그래야 내보낸 PNG에
/// 알파가 남아 게임 스프라이트로 바로 쓸 수 있다.
/// </summary>
[RequireComponent(typeof(RawImage))]
public sealed class DrawingCanvas : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public const int DefaultSize = 512;

    private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

    [SerializeField] private int textureSize = DefaultSize;
    [SerializeField] private int penRadius = 4;
    [SerializeField] private int crayonRadius = 13;
    [SerializeField] private int eraserRadius = 18;

    private RawImage target;
    private RectTransform rectTransform;
    private Texture2D texture;
    private Color32[] pixels;
    private DrawingHistory history;

    private BrushKind tool = BrushKind.Pen;
    private Color32 color = new Color32(30, 30, 30, 255);
    private WeaponPointKind? pointMode;
    private Vector2 grip = DefaultGrip;
    private Vector2 weaponCenter = DefaultCenter;
    private Vector2 tip = DefaultTip;
    private bool hasLastPoint;
    private int lastX;
    private int lastY;

    /// <summary>아무것도 정하지 않았으면 그림 한가운데를 잡는다.</summary>
    public static readonly Vector2 DefaultGrip = new Vector2(0.5f, 0.5f);

    /// <summary>기본 축은 "위로 뻗은" 그림 — 끝은 위쪽 가장자리 가운데다.</summary>
    public static readonly Vector2 DefaultTip = new Vector2(0.5f, 1f);

    /// <summary>기본 중심은 손잡이와 끝의 가운데.</summary>
    public static readonly Vector2 DefaultCenter = new Vector2(0.5f, 0.75f);

    public Texture2D Texture => texture;
    public BrushKind Tool => tool;
    public Color32 Color => color;
    public bool CanUndo => history != null && history.CanUndo;
    public bool CanRedo => history != null && history.CanRedo;

    /// <summary>
    /// 기준점을 찍는 중인가(무엇을 찍는 중인지). 이 동안에는 캔버스를 눌러도
    /// 칠하지 않는다 — 그림을 망치지 않고 자리만 옮길 수 있어야 한다.
    /// </summary>
    public WeaponPointKind? PointMode => pointMode;

    /// <summary>그립을 찍는 중인가 — 예전 이름을 쓰는 코드를 위한 준말.</summary>
    public bool GripMode => pointMode == WeaponPointKind.Grip;

    /// <summary>
    /// 무기를 잡는 자리. 그림 기준 정규화 좌표(0~1)이고 원점은 왼쪽 아래다 —
    /// 스프라이트 pivot과 같은 규약이라 그대로 구워 넣을 수 있다.
    /// </summary>
    public Vector2 Grip => grip;

    /// <summary>무기 몸통의 가운데 (정규화 0~1).</summary>
    public Vector2 WeaponCenter => weaponCenter;

    /// <summary>칼끝 — 손잡이→끝이 무기의 축이다 (정규화 0~1).</summary>
    public Vector2 Tip => tip;

    /// <summary>종류로 기준점을 읽는다.</summary>
    public Vector2 Point(WeaponPointKind kind) => kind switch
    {
        WeaponPointKind.Center => weaponCenter,
        WeaponPointKind.Tip => tip,
        _ => grip
    };

    /// <summary>그림이 바뀔 때마다 — 미리보기 갱신·버튼 활성화에 쓴다.</summary>
    public event System.Action Changed;

    /// <summary>기준점이 움직일 때마다 — 화면의 표시를 따라 옮기는 데 쓴다.</summary>
    public event System.Action<WeaponPointKind> PointChanged;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (texture != null)
        {
            return;
        }

        target = GetComponent<RawImage>();
        rectTransform = (RectTransform)transform;
        history = new DrawingHistory();

        int size = Mathf.Max(16, textureSize);
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Weapon Drawing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        pixels = new Color32[size * size];
        DrawingBrush.Fill(pixels, Transparent);
        Apply();

        target.texture = texture;
        // 투명한 곳도 눌러야 하므로 레이캐스트 대상은 유지한다
        target.raycastTarget = true;
    }

    public void SetTool(BrushKind newTool)
    {
        EnsureInitialized();
        tool = newTool;
        // 붓을 집으면 기준점 찍기는 끝난다 — 안 그러면 연필을 눌러도 계속 점만 옮긴다
        pointMode = null;
    }

    /// <summary>"그립" 도구 — 다음 클릭·드래그가 잡는 자리를 옮긴다.</summary>
    public void EnterGripMode() => EnterPointMode(WeaponPointKind.Grip);

    /// <summary>기준점 도구 — 다음 클릭·드래그가 해당 점을 옮긴다.</summary>
    public void EnterPointMode(WeaponPointKind kind)
    {
        EnsureInitialized();
        pointMode = kind;
        hasLastPoint = false;
    }

    public void SetColor(Color32 newColor)
    {
        EnsureInitialized();
        color = newColor;
        // 색을 고르면 지우개에서 자동으로 빠져나온다 — 안 그러면 색을 눌러도 계속 지워진다.
        // 채우기는 그대로 둔다 — 색을 바꿔 다시 채우는 흐름이 자연스럽다.
        if (tool == BrushKind.Eraser)
        {
            tool = BrushKind.Pen;
        }

        // 색을 고르는 것은 그리려는 뜻이다 — 기준점 찍기에서도 빠져나온다
        pointMode = null;
    }

    /// <summary>정규화 좌표(0~1, 왼쪽 아래 원점)로 그립을 옮긴다. 범위 밖은 잘라 넣는다.</summary>
    public void SetGrip(Vector2 normalized) => SetPoint(WeaponPointKind.Grip, normalized);

    /// <summary>정규화 좌표(0~1, 왼쪽 아래 원점)로 기준점을 옮긴다. 범위 밖은 잘라 넣는다.</summary>
    public void SetPoint(WeaponPointKind kind, Vector2 normalized)
    {
        EnsureInitialized();
        var clamped = new Vector2(
            Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
        if (clamped == Point(kind))
        {
            return;
        }

        switch (kind)
        {
            case WeaponPointKind.Center: weaponCenter = clamped; break;
            case WeaponPointKind.Tip: tip = clamped; break;
            default: grip = clamped; break;
        }

        PointChanged?.Invoke(kind);
    }

    public void Clear()
    {
        EnsureInitialized();
        history.Push(ToBytes(pixels));
        DrawingBrush.Fill(pixels, Transparent);
        Apply();
        Changed?.Invoke();
    }

    public void Undo()
    {
        EnsureInitialized();
        byte[] restored = history.Undo(ToBytes(pixels));
        if (restored == null)
        {
            return;
        }

        FromBytes(restored, pixels);
        Apply();
        Changed?.Invoke();
    }

    public void Redo()
    {
        EnsureInitialized();
        byte[] restored = history.Redo(ToBytes(pixels));
        if (restored == null)
        {
            return;
        }

        FromBytes(restored, pixels);
        Apply();
        Changed?.Invoke();
    }

    /// <summary>한 획도 안 그린 상태인가 — 빈 그림으로 무기를 만들지 못하게 막는 데 쓴다.</summary>
    public bool IsEmpty()
    {
        EnsureInitialized();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a != 0)
            {
                return false;
            }
        }

        return true;
    }

    public byte[] EncodeToPng()
    {
        EnsureInitialized();
        return texture.EncodeToPNG();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EnsureInitialized();
        if (pointMode.HasValue)
        {
            TryMovePoint(eventData);
            return;
        }

        if (!TryGetPixel(eventData, out int x, out int y))
        {
            return;
        }

        // 획 하나가 undo 한 단계 — 획을 <em>긋기 전</em> 상태를 쌓는다
        history.Push(ToBytes(pixels));

        if (tool == BrushKind.Fill)
        {
            // 채우기는 획이 아니라 한 번의 동작이다 — 드래그로 이어지지 않는다
            DrawingFill.Fill(pixels, texture.width, texture.height, x, y, color);
            hasLastPoint = false;
            Apply();
            Changed?.Invoke();
            return;
        }

        DrawingBrush.Stamp(pixels, texture.width, texture.height, x, y, CurrentBrush());
        lastX = x;
        lastY = y;
        hasLastPoint = true;
        Apply();
        Changed?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureInitialized();
        if (pointMode.HasValue)
        {
            // 끌어서 자리를 맞출 수 있어야 한다 — 한 번에 정확히 찍기는 어렵다
            TryMovePoint(eventData);
            return;
        }

        if (tool == BrushKind.Fill)
        {
            return;  // 채우기는 끌어서 이어 칠하지 않는다
        }

        if (!TryGetPixel(eventData, out int x, out int y))
        {
            // 캔버스 밖으로 나갔다 들어오면 그 사이를 잇지 않는다
            hasLastPoint = false;
            return;
        }

        if (hasLastPoint)
        {
            DrawingBrush.StampLine(
                pixels, texture.width, texture.height, lastX, lastY, x, y, CurrentBrush());
        }
        else
        {
            DrawingBrush.Stamp(pixels, texture.width, texture.height, x, y, CurrentBrush());
        }

        lastX = x;
        lastY = y;
        hasLastPoint = true;
        Apply();
        Changed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        hasLastPoint = false;
    }

    private BrushSettings CurrentBrush()
    {
        int radius = tool switch
        {
            BrushKind.Pen => penRadius,
            BrushKind.Crayon => crayonRadius,
            _ => eraserRadius
        };
        return new BrushSettings(tool, radius, color);
    }

    /// <summary>
    /// 찍는 중인 기준점을 포인터 자리로. 캔버스 밖으로 끌어도 가장자리에 붙여 둔다 —
    /// 칠하기와 달리 중간에 끊기면 점이 어디 있는지 알 수 없어진다.
    /// </summary>
    private void TryMovePoint(PointerEventData eventData)
    {
        if (pointMode.HasValue
            && TryGetNormalized(eventData, out Vector2 normalized, clampOutside: true))
        {
            SetPoint(pointMode.Value, normalized);
        }
    }

    private bool TryGetPixel(PointerEventData eventData, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (!TryGetNormalized(eventData, out Vector2 normalized, clampOutside: false))
        {
            return false;
        }

        x = Mathf.Clamp(
            Mathf.FloorToInt(normalized.x * texture.width), 0, texture.width - 1);
        y = Mathf.Clamp(
            Mathf.FloorToInt(normalized.y * texture.height), 0, texture.height - 1);
        return true;
    }

    /// <summary>
    /// 포인터를 캔버스 기준 정규화 좌표(0~1, 왼쪽 아래 원점)로.
    /// <paramref name="clampOutside"/>가 false면 캔버스 밖은 실패로 돌려준다.
    /// </summary>
    private bool TryGetNormalized(
        PointerEventData eventData, out Vector2 normalized, bool clampOutside)
    {
        normalized = Vector2.zero;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;
        if (!clampOutside && (u < 0f || u > 1f || v < 0f || v > 1f))
        {
            return false;
        }

        normalized = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        return true;
    }

    private void Apply()
    {
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private static byte[] ToBytes(Color32[] source)
    {
        var bytes = new byte[source.Length * 4];
        for (int i = 0; i < source.Length; i++)
        {
            int offset = i * 4;
            bytes[offset] = source[i].r;
            bytes[offset + 1] = source[i].g;
            bytes[offset + 2] = source[i].b;
            bytes[offset + 3] = source[i].a;
        }

        return bytes;
    }

    private static void FromBytes(byte[] bytes, Color32[] destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            int offset = i * 4;
            destination[i] = new Color32(
                bytes[offset],
                bytes[offset + 1],
                bytes[offset + 2],
                bytes[offset + 3]);
        }
    }
}
