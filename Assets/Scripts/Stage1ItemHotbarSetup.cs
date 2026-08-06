using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class Stage1ItemHotbarSetup
{
    private const string CanvasName = "Item Hotbar Canvas";
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public static void Create(
        GameObject player,
        Transform canvasParent,
        Sprite backgroundSprite,
        Sprite swordSprite)
    {
        Create(player, canvasParent, backgroundSprite, swordSprite, null);
    }

    public static void Create(
        GameObject player,
        Transform canvasParent,
        Sprite backgroundSprite,
        Sprite swordSprite,
        Sprite axeSprite)
    {
        Create(player, canvasParent, backgroundSprite, swordSprite, axeSprite, true);
    }

    /// <summary>
    /// <paramref name="useSampleLoadout"/>가 false면 샘플 무기 5종을 넣지 않는다.
    /// 던전은 대장간에서 만든 무기 하나만 들고 들어간다.
    /// </summary>
    public static void Create(
        GameObject player,
        Transform canvasParent,
        Sprite backgroundSprite,
        Sprite swordSprite,
        Sprite axeSprite,
        bool useSampleLoadout)
    {
        if (backgroundSprite == null)
        {
            throw new System.ArgumentNullException(nameof(backgroundSprite));
        }

        if (swordSprite == null)
        {
            throw new System.ArgumentNullException(nameof(swordSprite));
        }

        ItemHotbarController controller = player.AddComponent<ItemHotbarController>();
        if (useSampleLoadout)
        {
            controller.ConfigureSampleLoadout(
                swordSprite,
                axeSprite ?? swordSprite);
        }
        PlayerInventory inventory = controller.Inventory;
        EnsureEventSystem(canvasParent);

        var canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)); // 없으면 슬롯 호버(툴팁)가 이벤트를 못 받는다
        canvasObject.transform.SetParent(canvasParent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0f;

        ItemHotbarUIFactory.Create(canvasObject.transform, controller, backgroundSprite);

        PlayerWeaponController weaponController =
            player.GetComponent<PlayerWeaponController>();
        if (weaponController == null)
        {
            weaponController = player.AddComponent<PlayerWeaponController>();
        }
        weaponController.InitializeInventory(inventory);

        // 장착한 무기를 손에 그린다. 판정과 같은 인벤토리를 봐야 화면과 실제가 어긋나지 않는다.
        PlayerWeaponVisual weaponVisual = player.GetComponent<PlayerWeaponVisual>();
        if (weaponVisual == null)
        {
            weaponVisual = player.AddComponent<PlayerWeaponVisual>();
        }
        weaponVisual.InitializeInventory(inventory);
    }

    private static void EnsureEventSystem(Transform parent)
    {
        EventSystem[] eventSystems =
            Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        EventSystem eventSystem = eventSystems.Length > 0 ? eventSystems[0] : null;

        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
