using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerSwordShooterTests : InputTestFixture
{
    private const string SwordAssetPath = "Assets/Weapons/sword.png";

    private Keyboard keyboard;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    public override void TearDown()
    {
        foreach (PlayerSwordShooter shooter in Object.FindObjectsByType<PlayerSwordShooter>(
                     FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(shooter.gameObject);
        }

        foreach (SwordProjectile projectile in Object.FindObjectsByType<SwordProjectile>(
                     FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(projectile.gameObject);
        }

        base.TearDown();
    }

    [Test]
    public void SwordAsset_IsAnExactSourceCopyWithRequiredImportSettings()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourcePath = Path.Combine(projectRoot, "sword.png");
        string copyPath = Path.Combine(projectRoot, "Assets", "Weapons", "sword.png");

        Assert.That(File.Exists(sourcePath), Is.True);
        Assert.That(File.Exists(copyPath), Is.True);
        Assert.That(CalculateSha256(copyPath), Is.EqualTo(CalculateSha256(sourcePath)));

        TextureImporter importer = AssetImporter.GetAtPath(SwordAssetPath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f));
        Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(512));
        Assert.That(
            importer.textureCompression,
            Is.EqualTo(TextureImporterCompression.Uncompressed));
    }

    [Test]
    public void InspectorDefaults_AreConfiguredAndNegativeNumericValuesAreCorrected()
    {
        PlayerSwordShooter shooter = CreateShooter();
        var serializedShooter = new SerializedObject(shooter);

        Assert.That(serializedShooter.FindProperty("damage").intValue, Is.EqualTo(5));
        Assert.That(serializedShooter.FindProperty("shotsPerSecond").floatValue, Is.EqualTo(2f));
        Assert.That(serializedShooter.FindProperty("projectileSpeed").floatValue, Is.EqualTo(8f));
        Assert.That(serializedShooter.FindProperty("spinSpeed").floatValue, Is.EqualTo(720f));
        Assert.That(serializedShooter.FindProperty("projectileLifetime").floatValue, Is.EqualTo(4f));
        Assert.That(serializedShooter.FindProperty("spawnOffset").floatValue, Is.EqualTo(0.8f));
        Assert.That(serializedShooter.FindProperty("swordSprite").objectReferenceValue, Is.Null);
        Assert.That(GetMinAttribute("projectileSpeed").min, Is.EqualTo(0f));
        Assert.That(GetMinAttribute("spinSpeed").min, Is.EqualTo(0f));
        Assert.That(GetMinAttribute("projectileLifetime").min, Is.EqualTo(0f));
        Assert.That(GetMinAttribute("spawnOffset").min, Is.EqualTo(0f));

        serializedShooter.FindProperty("damage").intValue = -1;
        serializedShooter.FindProperty("shotsPerSecond").floatValue = -1f;
        serializedShooter.FindProperty("projectileSpeed").floatValue = -1f;
        serializedShooter.FindProperty("spinSpeed").floatValue = -1f;
        serializedShooter.FindProperty("projectileLifetime").floatValue = -1f;
        serializedShooter.FindProperty("spawnOffset").floatValue = -1f;
        serializedShooter.ApplyModifiedPropertiesWithoutUndo();
        InvokePrivateMethod(shooter, "OnValidate");
        serializedShooter.Update();

        Assert.That(serializedShooter.FindProperty("damage").intValue, Is.EqualTo(0));
        Assert.That(serializedShooter.FindProperty("shotsPerSecond").floatValue, Is.EqualTo(0.01f));
        Assert.That(serializedShooter.FindProperty("projectileSpeed").floatValue, Is.EqualTo(0f));
        Assert.That(serializedShooter.FindProperty("spinSpeed").floatValue, Is.EqualTo(0f));
        Assert.That(serializedShooter.FindProperty("projectileLifetime").floatValue, Is.EqualTo(0f));
        Assert.That(serializedShooter.FindProperty("spawnOffset").floatValue, Is.EqualTo(0f));
    }

    [Test]
    public void CalculateDirection_ArrowKeysProduceExactCardinalDirections()
    {
        AssertDirectionForKey(keyboard.leftArrowKey, Vector2.left);
        AssertDirectionForKey(keyboard.rightArrowKey, Vector2.right);
        AssertDirectionForKey(keyboard.upArrowKey, Vector2.up);
        AssertDirectionForKey(keyboard.downArrowKey, Vector2.down);
    }

    [Test]
    public void CalculateDirection_DiagonalArrowKeysAreNormalized()
    {
        Press(keyboard.rightArrowKey);
        Press(keyboard.upArrowKey);

        Vector2 direction = PlayerSwordShooter.CalculateDirection(keyboard);

        Assert.That(direction.x, Is.EqualTo(0.7071068f).Within(0.000001f));
        Assert.That(direction.y, Is.EqualTo(0.7071068f).Within(0.000001f));
    }

    [Test]
    public void CalculateDirection_WasdDoesNotAffectFiringDirection()
    {
        Press(keyboard.wKey);
        Press(keyboard.aKey);
        Press(keyboard.sKey);
        Press(keyboard.dKey);

        Assert.That(PlayerSwordShooter.CalculateDirection(keyboard), Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void ProcessInput_HeldRightFiresImmediatelyAndAtTheExactCooldownBoundary()
    {
        PlayerSwordShooter shooter = CreateShooter();
        SetNumericConfiguration(shooter, shotsPerSecond: 2f);
        Press(keyboard.rightArrowKey);

        shooter.ProcessInput(keyboard, 0f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        shooter.ProcessInput(keyboard, 0.49f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        shooter.ProcessInput(keyboard, 0.5f);
        Assert.That(ProjectileCount(), Is.EqualTo(2));
    }

    [Test]
    public void ProcessInput_AfterReleaseAndRepressWaitsForTheOriginalCooldown()
    {
        PlayerSwordShooter shooter = CreateShooter();
        SetNumericConfiguration(shooter, shotsPerSecond: 2f);
        Press(keyboard.rightArrowKey);
        shooter.ProcessInput(keyboard, 0f);

        Release(keyboard.rightArrowKey);
        shooter.ProcessInput(keyboard, 0.1f);
        Press(keyboard.rightArrowKey);
        shooter.ProcessInput(keyboard, 0.2f);

        Assert.That(ProjectileCount(), Is.EqualTo(1));

        shooter.ProcessInput(keyboard, 0.5f);

        Assert.That(ProjectileCount(), Is.EqualTo(2));
    }

    [Test]
    public void ProcessInput_OnlyFiresForSelectedSwordAndReselectionKeepsTheOriginalCooldown()
    {
        PlayerSwordShooter shooter = CreateShooter();
        PlayerInventory inventory = GetInventory(shooter);
        ItemHotbarController hotbar = shooter.gameObject.AddComponent<ItemHotbarController>();
        hotbar.Initialize(inventory);
        SetNumericConfiguration(shooter, shotsPerSecond: 2f);
        Press(keyboard.rightArrowKey);

        shooter.ProcessInput(keyboard, 0f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        Assert.That(inventory.TryAcquire(new ItemData("potion", "Potion", ItemKind.Item)), Is.True);
        Press(keyboard.digit2Key);
        hotbar.ProcessKeyboard(keyboard);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(1));
        shooter.ProcessInput(keyboard, 0.1f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        Press(keyboard.digit1Key);
        hotbar.ProcessKeyboard(keyboard);
        Assert.That(inventory.SelectedSlotIndex, Is.EqualTo(0));
        shooter.ProcessInput(keyboard, 0.2f);
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        shooter.ProcessInput(keyboard, 0.5f);
        Assert.That(ProjectileCount(), Is.EqualTo(2));
    }

    [Test]
    public void ProcessInput_NullInventoryEmptySlotZeroAndNonSwordItemDoNotFire()
    {
        Press(keyboard.rightArrowKey);

        PlayerSwordShooter nullInventoryShooter = new GameObject("Null inventory player")
            .AddComponent<PlayerSwordShooter>();
        nullInventoryShooter.ProcessInput(keyboard, 0f);

        PlayerSwordShooter emptySlotShooter = new GameObject("Empty slot player")
            .AddComponent<PlayerSwordShooter>();
        InitializeInventory(emptySlotShooter, new PlayerInventory());
        emptySlotShooter.ProcessInput(keyboard, 0f);

        PlayerSwordShooter nonSwordShooter = new GameObject("Non-sword player")
            .AddComponent<PlayerSwordShooter>();
        var nonSwordInventory = new PlayerInventory();
        Assert.That(nonSwordInventory.TryAcquire(new ItemData("axe", "Axe", ItemKind.Weapon)), Is.True);
        InitializeInventory(nonSwordShooter, nonSwordInventory);
        nonSwordShooter.ProcessInput(keyboard, 0f);

        Assert.That(ProjectileCount(), Is.EqualTo(0));
    }

    [Test]
    public void SpawnedProjectile_HasRequiredComponentsSpritePositionAndConfiguration()
    {
        PlayerSwordShooter shooter = CreateShooter();
        Sprite swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordAssetPath);
        Assert.That(swordSprite, Is.Not.Null);
        shooter.SwordSprite = swordSprite;
        shooter.transform.position = new Vector3(2f, 3f, 0f);
        SetNumericConfiguration(
            shooter,
            damage: 5,
            shotsPerSecond: 3f,
            projectileSpeed: 8f,
            spinSpeed: 720f,
            projectileLifetime: 4f,
            spawnOffset: 0.8f);
        Press(keyboard.rightArrowKey);

        shooter.ProcessInput(keyboard, 0f);

        SwordProjectile projectile = Object.FindAnyObjectByType<SwordProjectile>();
        Assert.That(projectile, Is.Not.Null);
        Assert.That(projectile.gameObject.name, Is.EqualTo("Sword Projectile"));
        Assert.That(projectile.transform.position, Is.EqualTo(new Vector3(2.8f, 3f, 0f)));

        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null);
        Assert.That(renderer.sprite, Is.SameAs(swordSprite));
        Assert.That(renderer.sortingOrder, Is.EqualTo(5));

        CapsuleCollider2D collider = projectile.GetComponent<CapsuleCollider2D>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True);

        Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        Assert.That(body.gravityScale, Is.EqualTo(0f));

        projectile.Advance(0.25f);
        Assert.That(projectile.transform.position.x, Is.EqualTo(4.8f).Within(0.0001f));
        Assert.That(projectile.transform.position.y, Is.EqualTo(3f).Within(0.0001f));
        Assert.That(
            Mathf.Repeat(projectile.transform.eulerAngles.z, 360f),
            Is.EqualTo(180f).Within(0.0001f));

        Collider2D ownerCollider = shooter.gameObject.AddComponent<BoxCollider2D>();
        EnemyHealth ownerHealth = shooter.gameObject.AddComponent<EnemyHealth>();
        InvokePrivateMethod(ownerHealth, "Awake");
        Assert.That(projectile.TryHit(ownerCollider), Is.False);
        Assert.That(ownerHealth.CurrentHealth, Is.EqualTo(20));
        Assert.That(GetPrivateField<int>(projectile, "damage"), Is.EqualTo(5));
    }

    [Test]
    public void SpawnedProjectile_ReceivesConfiguredLifetime()
    {
        PlayerSwordShooter shooter = CreateShooter();
        SetNumericConfiguration(shooter, projectileLifetime: 0.1f);
        Press(keyboard.rightArrowKey);
        shooter.ProcessInput(keyboard, 0f);
        SwordProjectile projectile = Object.FindAnyObjectByType<SwordProjectile>();

        Assert.That(GetPrivateField<float>(projectile, "remainingLifetime"), Is.EqualTo(0.1f));
    }

    private void AssertDirectionForKey(KeyControl key, Vector2 expected)
    {
        Press(key);
        Assert.That(PlayerSwordShooter.CalculateDirection(keyboard), Is.EqualTo(expected));
        Release(key);
    }

    private static PlayerSwordShooter CreateShooter()
    {
        PlayerSwordShooter shooter = new GameObject("Player").AddComponent<PlayerSwordShooter>();
        var inventory = new PlayerInventory();
        Assert.That(inventory.TryAcquire(new ItemData("sword", "Sword", ItemKind.Weapon)), Is.True);
        InitializeInventory(shooter, inventory);
        return shooter;
    }

    private static PlayerInventory GetInventory(PlayerSwordShooter shooter)
    {
        return GetPrivateField<PlayerInventory>(shooter, "inventory");
    }

    private static void InitializeInventory(PlayerSwordShooter shooter, PlayerInventory inventory)
    {
        MethodInfo initializeMethod = typeof(PlayerSwordShooter).GetMethod(
            "InitializeInventory",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(initializeMethod, Is.Not.Null);
        initializeMethod.Invoke(shooter, new object[] { inventory });
        Assert.That(
            GetPrivateField<PlayerInventory>(shooter, "inventory"),
            Is.SameAs(inventory));
    }

    private static int ProjectileCount()
    {
        return Object.FindObjectsByType<SwordProjectile>(
            FindObjectsInactive.Include).Length;
    }

    private static string CalculateSha256(string path)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return System.BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(path)))
                .Replace("-", string.Empty);
        }
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        target.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(target, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        return (T)target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(target);
    }

    private static MinAttribute GetMinAttribute(string fieldName)
    {
        MinAttribute attribute = typeof(PlayerSwordShooter)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetCustomAttribute<MinAttribute>();
        Assert.That(attribute, Is.Not.Null, fieldName + " must reject negative Inspector values");
        return attribute;
    }

    private static void SetNumericConfiguration(
        PlayerSwordShooter shooter,
        int? damage = null,
        float? shotsPerSecond = null,
        float? projectileSpeed = null,
        float? spinSpeed = null,
        float? projectileLifetime = null,
        float? spawnOffset = null)
    {
        var serializedShooter = new SerializedObject(shooter);
        SetIntIfPresent(serializedShooter, "damage", damage);
        SetFloatIfPresent(serializedShooter, "shotsPerSecond", shotsPerSecond);
        SetFloatIfPresent(serializedShooter, "projectileSpeed", projectileSpeed);
        SetFloatIfPresent(serializedShooter, "spinSpeed", spinSpeed);
        SetFloatIfPresent(serializedShooter, "projectileLifetime", projectileLifetime);
        SetFloatIfPresent(serializedShooter, "spawnOffset", spawnOffset);
        serializedShooter.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetIntIfPresent(SerializedObject target, string name, int? value)
    {
        if (value.HasValue)
        {
            target.FindProperty(name).intValue = value.Value;
        }
    }

    private static void SetFloatIfPresent(SerializedObject target, string name, float? value)
    {
        if (value.HasValue)
        {
            target.FindProperty(name).floatValue = value.Value;
        }
    }
}
