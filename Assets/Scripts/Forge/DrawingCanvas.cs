using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private bool hasLastPoint;
    private int lastX;
    private int lastY;

    public Texture2D Texture => texture;
    public BrushKind Tool => tool;
    public Color32 Color => color;
    public bool CanUndo => history != null && history.CanUndo;
    public bool CanRedo => history != null && history.CanRedo;

    /// <summary>그림이 바뀔 때마다 — 미리보기 갱신·버튼 활성화에 쓴다.</summary>
    public event System.Action Changed;

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
    }

    public void SetColor(Color32 newColor)
    {
        EnsureInitialized();
        color = newColor;
        // 색을 고르면 지우개에서 자동으로 빠져나온다 — 안 그러면 색을 눌러도 계속 지워진다
        if (tool == BrushKind.Eraser)
        {
            tool = BrushKind.Pen;
        }
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
        if (!TryGetPixel(eventData, out int x, out int y))
        {
            return;
        }

        // 획 하나가 undo 한 단계 — 획을 <em>긋기 전</em> 상태를 쌓는다
        history.Push(ToBytes(pixels));

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

    private bool TryGetPixel(PointerEventData eventData, out int x, out int y)
    {
        x = 0;
        y = 0;

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
        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            return false;
        }

        x = Mathf.Clamp(Mathf.FloorToInt(u * texture.width), 0, texture.width - 1);
        y = Mathf.Clamp(Mathf.FloorToInt(v * texture.height), 0, texture.height - 1);
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
