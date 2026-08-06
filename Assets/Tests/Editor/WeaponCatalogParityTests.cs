using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 카탈로그 ↔ 구현 대조 — 이 시스템에서 가장 중요한 테스트.
///
/// 효과·궤도를 덜어내거나 더할 때 고치는 곳은 세 곳이다:
/// 백엔드 카탈로그 JSON(+동기화), 구현 파일, 레지스트리 등록 한 줄.
/// 셋 중 하나만 빠뜨려도 여기서 즉시 무너진다 — 게임에 들어가서야 발견하는 일이 없게.
/// </summary>
public sealed class WeaponCatalogParityTests
{
    [Test]
    public void EveryCatalogEffectHasAnImplementation()
    {
        WeaponCatalog catalog = WeaponCatalog.Load();
        Assert.That(catalog, Is.Not.Null, "카탈로그가 없다 — tools/sync-catalog.py 를 실행할 것");

        List<string> problems = EffectRegistry.CreateDefault().ValidateAgainstCatalog(catalog);

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    [Test]
    public void EveryCatalogDeliveryHasAnImplementation()
    {
        WeaponCatalog catalog = WeaponCatalog.Load();
        Assert.That(catalog, Is.Not.Null);

        List<string> problems = DeliveryRegistry.CreateDefault().ValidateAgainstCatalog(catalog);

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    [Test]
    public void ValidationCatchesAMissingImplementation()
    {
        // 대조 검사 자체가 살아 있는지 — 빈 레지스트리면 모든 효과가 문제로 잡혀야 한다
        WeaponCatalog catalog = WeaponCatalog.Load();
        var empty = new EffectRegistry();

        List<string> problems = empty.ValidateAgainstCatalog(catalog);

        Assert.That(problems.Count, Is.EqualTo(catalog.EffectIds.Count));
    }

    [Test]
    public void ValidationCatchesAnOrphanImplementation()
    {
        WeaponCatalog catalog = WeaponCatalog.Load();
        var registry = EffectRegistry.CreateDefault();
        registry.Register(new OrphanAction());

        List<string> problems = registry.ValidateAgainstCatalog(catalog);

        Assert.That(problems, Has.Some.Contains("고아_효과"));
    }

    private sealed class OrphanAction : IEffectAction
    {
        public string EffectId => "고아_효과";

        public void Execute(EffectContext context, ParamSet parameters)
        {
        }
    }
}
