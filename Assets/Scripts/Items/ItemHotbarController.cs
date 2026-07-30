using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ItemHotbarController : MonoBehaviour
{
    [SerializeField]
    private Sprite startingSwordSprite;

    [SerializeField]
    private Sprite startingAxeSprite;

    private PlayerInventory inventory;

    public PlayerInventory Inventory
    {
        get
        {
            EnsureInventory();
            return inventory;
        }
    }

    public static int SlotIndexForNumber(int number)
    {
        return number >= 1 && number <= 6 ? number - 1 : -1;
    }

    public void Initialize(PlayerInventory newInventory)
    {
        inventory = newInventory ?? new PlayerInventory();
        EnsureStartingWeapons();
    }

    public void ConfigureStartingSword(Sprite swordSprite)
    {
        startingSwordSprite = swordSprite;
        EnsureInventory();
    }

    public void ConfigureStartingWeapons(Sprite swordSprite, Sprite axeSprite)
    {
        startingSwordSprite = swordSprite;
        startingAxeSprite = axeSprite;
        EnsureInventory();
    }

    private void Awake()
    {
        EnsureInventory();
    }

    private void Update()
    {
        ProcessKeyboard(Keyboard.current);
    }

    public void ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return;
        }

        PlayerInventory currentInventory = Inventory;
        for (int number = 1; number <= 6; number++)
        {
            if (keyboard[(Key)((int)Key.Digit1 + number - 1)].wasPressedThisFrame)
            {
                currentInventory.SelectSlot(SlotIndexForNumber(number));
                return;
            }
        }
    }

    private void EnsureInventory()
    {
        if (inventory == null)
        {
            inventory = new PlayerInventory();
        }

        EnsureStartingWeapons();

        PlayerSwordShooter shooter = GetComponent<PlayerSwordShooter>();
        if (shooter != null)
        {
            shooter.InitializeInventory(inventory);
        }

        PlayerAxeAttacker axeAttacker = GetComponent<PlayerAxeAttacker>();
        if (axeAttacker != null)
        {
            axeAttacker.InitializeInventory(inventory);
        }
    }

    private void EnsureStartingWeapons()
    {
        if (startingSwordSprite != null)
        {
            inventory.EnsureUniqueItemInSlotZero(
                new ItemData("sword", "Sword", ItemKind.Weapon, startingSwordSprite));
        }

        if (startingAxeSprite != null)
        {
            inventory.EnsureUniqueItemInSlot(
                1,
                new ItemData("axe", "Axe", ItemKind.Weapon, startingAxeSprite));
        }

        // 무기 만들기 화면에서 확정한 무기가 있으면 3번 칸에 들어간다.
        // 안 만들고 건너뛰었으면 아무 일도 일어나지 않는다.
        if (ForgedWeapon.HasWeapon)
        {
            inventory.EnsureUniqueItemInSlot(
                ForgedWeapon.SlotIndex,
                ForgedWeapon.ToItemData());
        }
    }
}
