using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>트리거 발동기 — 어떤 트리거가 언제 효과를 터뜨리는지.</summary>
public sealed class EffectRunnerTests
{
    private GameObject player;
    private EffectRunner runner;
    private RecordingAction recorder;
    private EffectRegistry registry;

    /// <summary>실행 기록만 남기는 가짜 효과 — 카탈로그의 fire_dot 자리에 꽂는다.</summary>
    private sealed class RecordingAction : IEffectAction
    {
        public readonly List<EffectContext> Calls = new List<EffectContext>();

        public string EffectId => "fire_dot";

        public void Execute(EffectContext context, ParamSet parameters)
        {
            Calls.Add(context);
        }
    }

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("player");
        runner = player.AddComponent<EffectRunner>();
        recorder = new RecordingAction();
        registry = new EffectRegistry();
        registry.Register(recorder);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (EnemyHealth leftover in Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(leftover.gameObject);
        }

        Object.DestroyImmediate(player);
    }

    private void Configure(string triggerId, params (string key, float value)[] entries)
    {
        var map = new Dictionary<string, float>();
        foreach ((string key, float value) in entries)
        {
            map[key] = value;
        }

        var loadout = new WeaponLoadout(
            null,
            new DeliverySpec("straight", ParamSet.Empty),
            new[] { new EffectSpec("fire_dot", triggerId, new ParamSet(map)) });
        runner.Configure(loadout, registry);
    }

    private static EnemyHealth MakeEnemy(Vector2 position, int hp = 100)
    {
        var enemyObject = new GameObject("enemy");
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(hp);
        Physics2D.SyncTransforms();
        return health;
    }

    [Test]
    public void OnHitFiresWhenTheAttackLands()
    {
        Configure("on_hit");
        EnemyHealth enemy = MakeEnemy(Vector2.zero);

        runner.NotifyHit(enemy, Vector2.zero, Vector2.right);

        Assert.That(recorder.Calls, Has.Count.EqualTo(1));
        Assert.That(recorder.Calls[0].Target, Is.SameAs(enemy));
    }

    [Test]
    public void OnKillFiresOnlyWhenTheTargetDies()
    {
        Configure("on_kill");
        EnemyHealth alive = MakeEnemy(Vector2.zero, hp: 100);

        runner.NotifyHit(alive, Vector2.zero, Vector2.right);
        Assert.That(recorder.Calls, Is.Empty, "살아 있으면 발동하지 않는다");

        // 죽은 대상은 이미 파괴 예약이라 null로 들어온다 — EnemyHealth.TakeDamage가
        // 0에서 Destroy를 부르는데, EditMode에서 그걸 실제로 부르면 에러가 난다.
        runner.NotifyHit(null, new Vector2(3f, 0f), Vector2.right);

        Assert.That(recorder.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public void AfterSecondsFiresOnceTheDelayPasses()
    {
        Configure("after_seconds", ("delaySeconds", 1f));

        runner.NotifyAttack(Vector2.zero, Vector2.right, currentTime: 10f);
        MakeEnemy(new Vector2(0.5f, 0f)); // 지연 발동이 대상을 찾을 적

        runner.Tick(10.5f);
        Assert.That(recorder.Calls, Is.Empty, "아직 1초가 안 지났다");

        runner.Tick(11.1f);
        Assert.That(recorder.Calls, Has.Count.EqualTo(1));

        runner.Tick(12f);
        Assert.That(recorder.Calls, Has.Count.EqualTo(1), "예약은 한 번만 터진다");
    }

    [Test]
    public void OnCooldownFiresRepeatedly()
    {
        Configure("on_cooldown", ("cooldownSeconds", 2f));
        MakeEnemy(new Vector2(0.5f, 0f));

        runner.Tick(0f);   // 첫 호출 — 쿨다운 시작만
        runner.Tick(1f);
        Assert.That(recorder.Calls, Is.Empty);

        runner.Tick(2.1f);
        Assert.That(recorder.Calls, Has.Count.EqualTo(1));

        runner.Tick(4.2f);
        Assert.That(recorder.Calls, Has.Count.EqualTo(2));
    }

    [Test]
    public void TargetlessTriggerSkipsWhenNoEnemyIsNear()
    {
        // 대상이 필요한 효과(fire_dot)가 대상 없는 트리거로 걸렸는데 주변에 적이 없다
        Configure("after_seconds", ("delaySeconds", 0.5f));

        runner.NotifyAttack(Vector2.zero, Vector2.right, currentTime: 0f);
        runner.Tick(1f);

        Assert.That(recorder.Calls, Is.Empty);
    }

    [Test]
    public void ReconfigureClearsPendingWork()
    {
        Configure("after_seconds", ("delaySeconds", 1f));
        runner.NotifyAttack(Vector2.zero, Vector2.right, currentTime: 0f);
        MakeEnemy(new Vector2(0.5f, 0f));

        // 무기를 바꿔 들었다 — 이전 무기의 예약이 새 무기로 터지면 안 된다
        Configure("on_hit");
        runner.Tick(2f);

        Assert.That(recorder.Calls, Is.Empty);
    }
}
