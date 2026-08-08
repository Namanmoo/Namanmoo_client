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
        // 샘플 로드아웃 뒤에 넣는다 — 먼저 넣으면 샘플이 같은 칸을 덮어쓴다.
        EnsureForgedWeapon();

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

            // [프로토타입] 스페이스+화살표 임시 스킬 — 추후 제거 예정
            PrototypeSkillController skill = GetComponent<PrototypeSkillController>();
            if (skill == null)
            {
                skill = gameObject.AddComponent<PrototypeSkillController>();
            }
            skill.InitializeInventory(inventory);
        }

        // 손에 든 그림도 같은 인벤토리를 봐야 한다. 이 재연결이 없으면 씬을 저장했다
        // 다시 열었을 때 참조가 끊긴 채로 남아 무기가 영영 안 그려진다 —
        // 굽는 시점에만 묶어 두면 직렬화를 못 넘는다.
        PlayerWeaponVisual weaponVisual = GetComponent<PlayerWeaponVisual>();
        if (weaponVisual != null)
        {
            weaponVisual.InitializeInventory(inventory);
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
    }

    /// <summary>
    /// 무기 만들기 화면에서 확정한 무기를 3번 칸에 넣는다.
    /// 안 만들고 건너뛰었으면 아무 일도 일어나지 않는다.
    ///
    /// 시작 무기와 떼어 놓는다 — 예전엔 <see cref="EnsureStartingWeapons"/> 안에 있었고
    /// 그 함수는 샘플 로드아웃일 때 아예 호출되지 않아서, 만든 무기가 조용히 사라졌다.
    /// </summary>
    private void EnsureForgedWeapon()
    {
        if (!ForgedWeapon.HasWeapon)
        {
            return;
        }

        inventory.EnsureUniqueItemInSlot(
            ForgedWeapon.SlotIndex,
            ForgedWeapon.ToItemData());
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
