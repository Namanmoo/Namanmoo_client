using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발 빌드 전용 — 장착한 무기의 궤도·효과를 키로 즉석 교체한다.
/// 대장간을 거치지 않고 스테이지 안에서 모든 조합을 눈으로 확인하기 위한 도구다.
///
///   9 : 궤도 전환 (장착 무기 분류에 맞는 것만 순환)
///   0 : 효과 전환 (없음 → 카탈로그 순환, 트리거는 명중 시)
///
/// 릴리스 빌드에는 붙지 않는다 — <see cref="PlayerWeaponController"/>가
/// <c>Debug.isDebugBuild</c>일 때만 붙여 준다.
/// </summary>
public sealed class WeaponDebugSwitcher : MonoBehaviour
{
    /// <summary>효과 순환의 첫 자리 — 효과 없음.</summary>
    public const string NoEffect = "없음";

    private WeaponCatalog catalog;
    private ItemHotbarController hotbar;
    private string status = "9: 궤도 전환  0: 효과 전환";
    private GUIStyle labelStyle;

    private void Awake()
    {
        catalog = WeaponCatalog.Load();
        hotbar = GetComponent<ItemHotbarController>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || catalog == null)
        {
            return;
        }

        if (keyboard.digit9Key.wasPressedThisFrame)
        {
            CycleDelivery();
        }

        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            CycleEffect();
        }
    }

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle
            {
                fontSize = 16,
                // IMGUI 기본 폰트에는 WebGL에서 한글 글리프가 없다
                font = Resources.Load<Font>(WeaponTooltipView.FontResource),
            };
            labelStyle.normal.textColor = Color.white;
        }

        var rect = new Rect(8f, Screen.height - 32f, 760f, 24f);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f); // 밝은 배경 위에서도 읽히게
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, rect.width, rect.height), status, labelStyle);
    }

    private void CycleDelivery()
    {
        if (!TryGetEquipped(out ItemData item, out WeaponLoadout loadout))
        {
            return;
        }

        string categoryId = CategoryIdOf(loadout);
        List<string> ids = DeliveryIdsFor(catalog, categoryId);
        string next = NextAfter(ids, loadout.Delivery?.DeliveryId);
        if (next == null)
        {
            return;
        }

        var delivery = new DeliverySpec(next, MidParams(catalog.Delivery(next)));
        Replace(item, new WeaponLoadout(loadout.Definition, delivery, loadout.Effects));

        status = $"궤도 → {catalog.Delivery(next)?.displayName_ko ?? next}"
            + $"   (효과: {WeaponSummary.Effects(EquippedLoadout()) ?? ""})";
    }

    private void CycleEffect()
    {
        if (!TryGetEquipped(out ItemData item, out WeaponLoadout loadout))
        {
            return;
        }

        string categoryId = CategoryIdOf(loadout);
        List<string> ids = EffectIdsFor(catalog, categoryId);
        string current = loadout.Effects.Count > 0 ? loadout.Effects[0].EffectId : NoEffect;
        string next = NextAfter(ids, current);
        if (next == null)
        {
            return;
        }

        // 검기는 맞아야 나가는 게 아니라 공격마다 나가야 한다
        string trigger = next == "blade_wave" ? "on_attack" : "on_hit";
        IReadOnlyList<EffectSpec> effects = next == NoEffect
            ? WeaponLoadout.NoEffects
            : new[] { new EffectSpec(next, trigger, MidParams(catalog.Effect(next))) };

        Replace(item, new WeaponLoadout(loadout.Definition, loadout.Delivery, effects));

        string triggerName = catalog.Trigger(trigger)?.displayName_ko ?? trigger;
        status = next == NoEffect
            ? "효과 → 없음"
            : $"효과 → {triggerName} · {catalog.Effect(next)?.displayName_ko ?? next}";
    }

    // ── 계산부 — EditMode 테스트로 덮는다 ─────────────────────

    /// <summary>분류에 맞는 궤도 id들 — 카탈로그 순서.</summary>
    public static List<string> DeliveryIdsFor(WeaponCatalog catalog, string categoryId)
    {
        var ids = new List<string>();
        foreach (CatalogEntry entry in catalog.DeliveriesFor(categoryId))
        {
            ids.Add(entry.id);
        }

        return ids;
    }

    /// <summary>"없음"으로 시작하는 효과 id 순환 목록.</summary>
    public static List<string> EffectIdsFor(WeaponCatalog catalog, string categoryId)
    {
        var ids = new List<string> { NoEffect };
        foreach (CatalogEntry entry in catalog.EffectsFor(categoryId))
        {
            ids.Add(entry.id);
        }

        return ids;
    }

    /// <summary>목록에서 current 다음 항목 — 끝이면 처음으로. 못 찾으면 첫 항목.</summary>
    public static string NextAfter(IReadOnlyList<string> ids, string current)
    {
        if (ids == null || ids.Count == 0)
        {
            return null;
        }

        for (int index = 0; index < ids.Count; index++)
        {
            if (ids[index] == current)
            {
                return ids[(index + 1) % ids.Count];
            }
        }

        return ids[0];
    }

    /// <summary>파라미터 전부 범위 가운데값 — 효과가 눈에 띄되 극단은 아니게.</summary>
    public static ParamSet MidParams(CatalogEntry entry)
    {
        if (entry?.@params == null || entry.@params.Length == 0)
        {
            return ParamSet.Empty;
        }

        var values = new Dictionary<string, float>();
        foreach (CatalogParam param in entry.@params)
        {
            values[param.key] = param.Clamp((param.min + param.max) * 0.5f);
        }

        return new ParamSet(values);
    }

    // ── 장착 무기 교체 ────────────────────────────────────────

    private static string CategoryIdOf(WeaponLoadout loadout)
    {
        return loadout.Definition.Category == WeaponCategory.Melee ? "melee" : "ranged";
    }

    private bool TryGetEquipped(out ItemData item, out WeaponLoadout loadout)
    {
        item = hotbar != null ? hotbar.Inventory?.EquippedItem : null;
        loadout = item?.Loadout;
        if (loadout?.Definition == null)
        {
            status = "무기를 장착하면 9/0 키로 궤도·효과를 바꿀 수 있습니다";
            return false;
        }

        return true;
    }

    private WeaponLoadout EquippedLoadout()
    {
        return hotbar != null ? hotbar.Inventory?.EquippedItem?.Loadout : null;
    }

    private void Replace(ItemData original, WeaponLoadout loadout)
    {
        PlayerInventory inventory = hotbar.Inventory;
        inventory.ReplaceSlot(
            inventory.SelectedSlotIndex,
            new ItemData(loadout, original.Icon, original.DisplayName));
    }
}
