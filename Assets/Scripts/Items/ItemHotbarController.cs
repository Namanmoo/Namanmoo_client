using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ItemHotbarController : MonoBehaviour
{
    [SerializeField]
    private Sprite startingSwordSprite;

    [SerializeField]
    private Sprite startingAxeSprite;

    [SerializeField]
    private bool useSampleLoadout;

    [SerializeField]
    private Sprite sampleProjectileSprite;

    [SerializeField]
    private Sprite sampleAxeSprite;

    private PlayerInventory inventory;
    private WeaponDefinition[] sampleWeapons;
    private bool sampleLoadoutApplied;

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

    public void ConfigureSampleWeapons(WeaponDefinition[] weapons)
    {
        EnsureInventory();
        if (weapons == null)
        {
            return;
        }

        for (int index = 0; index < weapons.Length && index < 6; index++)
        {
            WeaponDefinition weapon = weapons[index];
            if (weapon != null && weapon.IsValid)
            {
                inventory.EnsureUniqueItemInSlot(index, new ItemData(weapon));
            }
        }
        inventory.SelectSlot(0);
    }

    public void ConfigureSampleLoadout(Sprite projectileSprite, Sprite axeSprite)
    {
        useSampleLoadout = true;
        sampleProjectileSprite = projectileSprite;
        sampleAxeSprite = axeSprite;
        sampleWeapons = null;
        sampleLoadoutApplied = false;
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
            sampleLoadoutApplied = false;
        }

        if (!useSampleLoadout)
        {
            EnsureStartingWeapons();
        }
        EnsureSampleLoadout();

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

        PlayerWeaponController weaponController = GetComponent<PlayerWeaponController>();
        if (weaponController == null && useSampleLoadout)
        {
            weaponController = gameObject.AddComponent<PlayerWeaponController>();
        }

        if (weaponController != null)
        {
            weaponController.InitializeInventory(inventory);
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

    private void EnsureSampleLoadout()
    {
        if (!useSampleLoadout || sampleLoadoutApplied)
        {
            return;
        }

        if (sampleWeapons == null)
        {
            sampleWeapons = SampleWeaponFactory.Create(
                sampleProjectileSprite,
                sampleAxeSprite ?? sampleProjectileSprite);
        }

        for (int index = 0; index < sampleWeapons.Length; index++)
        {
            inventory.EnsureUniqueItemInSlot(index, new ItemData(sampleWeapons[index]));
        }
        inventory.SelectSlot(0);
        sampleLoadoutApplied = true;
    }
}
