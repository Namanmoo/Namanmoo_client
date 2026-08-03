using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PlayerDeathScreenUIFactoryTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Object.DestroyImmediate(eventSystem.gameObject);
        }
    }

    [Test]
    public void Create_BuildsHiddenDeathMenuAboveTransparentBlackOverlay()
    {
        root = new GameObject(nameof(PlayerDeathScreenUIFactoryTests));

        PlayerDeathScreenView view =
            PlayerDeathScreenUIFactory.Create(root.transform);

        Canvas canvas = view.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Text message = view.Menu.transform.Find("Message").GetComponent<Text>();

        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(canvas.sortingOrder, Is.EqualTo(100));
        Assert.That(
            scaler.referenceResolution,
            Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(
            view.FadeOverlay.color,
            Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
        Assert.That(view.Menu.activeSelf, Is.False);
        Assert.That(message.text, Is.EqualTo("이번에도 틀렸나..."));
        Assert.That(
            view.TitleButton.GetComponentInChildren<Text>().text,
            Is.EqualTo("타이틀화면으로 돌아가기"));
        Assert.That(
            view.RestartButton.GetComponentInChildren<Text>().text,
            Is.EqualTo("처음부터 다시하기"));
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        Assert.That(eventSystem, Is.Not.Null);
        Assert.That(
            eventSystem.GetComponent<InputSystemUIInputModule>(),
            Is.Not.Null);
    }
}
