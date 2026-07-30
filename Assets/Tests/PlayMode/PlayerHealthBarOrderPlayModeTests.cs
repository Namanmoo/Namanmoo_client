using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// 체력 바가 컴포넌트 순서에 흔들리지 않는지.
///
/// 실제로 겪은 버그: 던전 씬을 다시 만들었더니 체력 바가 <c>PlayerHealth.Awake</c>보다
/// 먼저 켜져 아직 0인 값을 읽었고, 그 뒤로 갱신 신호가 없어 <b>0/20으로 시작</b>했다.
/// 맞기 전까지는 계속 0으로 보인다.
/// </summary>
public sealed class PlayerHealthBarOrderPlayModeTests
{
    private GameObject player;
    private GameObject ui;

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
        Object.DestroyImmediate(ui);
    }

    private (PlayerHealth, Text) Build(bool connectViewFirst)
    {
        var canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>();

        var textObject = new GameObject("Health Text");
        textObject.transform.SetParent(canvas.transform, false);
        Text text = textObject.AddComponent<Text>();

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(canvas.transform, false);
        Image fill = fillObject.AddComponent<Image>();

        ui = canvas;

        if (connectViewFirst)
        {
            // 체력 바가 먼저 켜지는 순서를 흉내 낸다
            var viewObject = new GameObject("Health Bar");
            viewObject.transform.SetParent(canvas.transform, false);
            var view = viewObject.AddComponent<PlayerHealthBarView>();

            player = new GameObject("Player");
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            view.Initialize(health, text, fill);
            return (health, text);
        }

        player = new GameObject("Player");
        PlayerHealth first = player.AddComponent<PlayerHealth>();

        var lateObject = new GameObject("Health Bar");
        lateObject.transform.SetParent(canvas.transform, false);
        var lateView = lateObject.AddComponent<PlayerHealthBarView>();
        lateView.Initialize(first, text, fill);
        return (first, text);
    }

    [UnityTest]
    public IEnumerator TheBarShowsFullHealthWhenItConnectsBeforeAwakeRuns()
    {
        (PlayerHealth health, Text text) = Build(connectViewFirst: true);
        yield return null;

        Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
        Assert.That(text.text, Is.EqualTo($"{health.MaxHealth}/{health.MaxHealth}"),
            "체력 바가 0으로 굳었다");
    }

    [UnityTest]
    public IEnumerator TheBarShowsFullHealthWhenItConnectsAfterAwakeRuns()
    {
        (PlayerHealth health, Text text) = Build(connectViewFirst: false);
        yield return null;

        Assert.That(text.text, Is.EqualTo($"{health.MaxHealth}/{health.MaxHealth}"));
    }

    [UnityTest]
    public IEnumerator TheBarStillFollowsDamage()
    {
        (PlayerHealth health, Text text) = Build(connectViewFirst: true);
        yield return null;

        health.TakeDamage(5);
        yield return null;

        Assert.That(text.text, Is.EqualTo($"{health.MaxHealth - 5}/{health.MaxHealth}"));
    }
}
