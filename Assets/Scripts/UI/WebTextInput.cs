using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// WebGL에서 한글이 입력되게 한다.
///
/// 유니티 WebGL은 캔버스에서 키 코드를 읽는데 한글은 키 코드로 오지 않는다 — 조합 중에는
/// keyCode 229만 오고 완성된 글자는 편집 가능한 DOM 요소에만 간다. 그래서 칸을 누르면
/// <b>그 자리에 진짜 <c>&lt;input&gt;</c>을 겹쳐</b> 브라우저와 IME가 글자를 만들게 하고,
/// 값만 받아 <see cref="InputField"/>에 넣는다. 일본어·중국어도 같이 해결된다.
///
/// 에디터와 다른 플랫폼에서는 아무 일도 하지 않는다 — 거기서는 원래 잘 된다.
/// </summary>
[RequireComponent(typeof(InputField))]
public sealed class WebTextInput : MonoBehaviour, IPointerClickHandler
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void NaManMooOpenText(
        string owner, string value, string placeholder,
        float x, float y, float width, float height, int maxLength);

    [DllImport("__Internal")]
    private static extern void NaManMooCloseText();

    [DllImport("__Internal")]
    private static extern int NaManMooIsTextOpen();
#endif

    private InputField field;
    private Canvas canvas;

    /// <summary>브라우저 입력창이 떠 있는 동안 참.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>이 플랫폼에서 웹 입력창을 쓰는가.</summary>
    public static bool IsSupported =>
#if UNITY_WEBGL && !UNITY_EDITOR
        true;
#else
        false;
#endif

    private void Awake()
    {
        field = GetComponent<InputField>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Open();
    }

    /// <summary>브라우저 입력창을 이 칸 자리에 띄운다.</summary>
    public void Open()
    {
        if (!IsSupported || field == null || IsOpen)
        {
            return;
        }

        Rect place = NormalizedPlace();
        if (place.width <= 0f || place.height <= 0f)
        {
            return;
        }

        string hint = field.placeholder is Text placeholder ? placeholder.text : string.Empty;
        IsOpen = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        NaManMooOpenText(
            gameObject.name, field.text ?? string.Empty, hint ?? string.Empty,
            place.x, place.y, place.width, place.height, field.characterLimit);
#endif

        // 유니티 쪽 칸은 커서만 깜박이고 글자는 못 받는다. 혼란스러우니 선택을 푼다.
        field.DeactivateInputField();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        NaManMooCloseText();
#else
        IsOpen = false;
#endif
    }

    /// <summary>이 칸이 화면에서 차지하는 자리를 0~1로.</summary>
    public Rect NormalizedPlace()
    {
        var rect = (RectTransform)transform;
        Rect pixels = ScreenRectOf.PixelsOf(rect, ScreenRectOf.CameraFor(canvas));
        return ScreenRectOf.Normalize(pixels, new Vector2(Screen.width, Screen.height));
    }

    // ── 자바스크립트가 부르는 것들 (SendMessage) ────────────────────

    /// <summary>글자가 바뀔 때마다. 조합이 끝난 글자만 온다.</summary>
    public void OnWebTextChanged(string value)
    {
        if (field == null)
        {
            return;
        }

        // onValueChanged 는 그대로 울려야 미리보기나 검증이 따라온다
        field.text = value ?? string.Empty;
    }

    /// <summary>입력창이 닫혔을 때. "1"이면 확정, "0"이면 취소다.</summary>
    public void OnWebTextClosed(string committed)
    {
        IsOpen = false;

        if (field != null && committed == "1")
        {
            field.onEndEdit?.Invoke(field.text);
        }
    }

    private void OnDisable()
    {
        // 화면을 떠나는데 입력창만 남으면 게임 위에 떠 있는 유령이 된다
        Close();
    }
}
