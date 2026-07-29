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
        if (backgroundSprite == null)
        {
            throw new System.ArgumentNullException(nameof(backgroundSprite));
        }

        if (swordSprite == null)
        {
            throw new System.ArgumentNullException(nameof(swordSprite));
        }

        ItemHotbarController controller = player.AddComponent<ItemHotbarController>();
        if (axeSprite == null)
        {
            controller.ConfigureStartingSword(swordSprite);
        }
        else
        {
            controller.ConfigureStartingWeapons(swordSprite, axeSprite);
        }
        PlayerInventory inventory = controller.Inventory;
        EnsureEventSystem(canvasParent);

        var canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(canvasParent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0f;

        ItemHotbarUIFactory.Create(canvasObject.transform, controller, backgroundSprite);

        PlayerSwordShooter shooter = player.GetComponent<PlayerSwordShooter>();
        if (shooter == null)
        {
            throw new System.InvalidOperationException(
                "Stage1ItemHotbarSetup requires PlayerSwordShooter on the player.");
        }

        shooter.InitializeInventory(inventory);

        PlayerAxeAttacker axeAttacker = player.GetComponent<PlayerAxeAttacker>();
        if (axeSprite != null && axeAttacker == null)
        {
            throw new System.InvalidOperationException(
                "Stage1ItemHotbarSetup requires PlayerAxeAttacker when an axe Sprite is provided.");
        }

        if (axeAttacker != null)
        {
            axeAttacker.InitializeInventory(inventory);
        }
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
