using NUnit.Framework;
using UnityEngine;

public sealed class CameraShakeTests
{
    private GameObject cameraObject;
    private CameraShake shake;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("shake test", typeof(Camera), typeof(CameraShake));
        shake = cameraObject.GetComponent<CameraShake>();
    }

    [TearDown]
    public void TearDown()
    {
        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void NoTriggerMeansNoOffsetAndNotShaking()
    {
        Assert.That(shake.IsShaking, Is.False);
        Assert.That(shake.CurrentOffset, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TriggerStartsAShakeWithOffsetBoundedByIntensity()
    {
        shake.Trigger(intensity: 1f, duration: 1f);
        shake.Tick(0f);

        Assert.That(shake.IsShaking, Is.True);
        Assert.That(shake.CurrentOffset.magnitude, Is.LessThanOrEqualTo(1f));
    }

    [Test]
    public void ShakeStopsExactlyAtTheEndOfItsDuration()
    {
        shake.Trigger(intensity: 1f, duration: 1f);
        shake.Tick(1f);

        Assert.That(shake.IsShaking, Is.False);
        Assert.That(shake.CurrentOffset, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void OffsetMagnitudeShrinksAsTheShakeNearsItsEnd()
    {
        shake.Trigger(intensity: 2f, duration: 1f);
        shake.Tick(0.9f);

        // 남은 시간 10% => 세기도 최대 10%로 줄어 있어야 한다
        Assert.That(shake.CurrentOffset.magnitude, Is.LessThanOrEqualTo(0.2f));
    }

    [Test]
    public void RetriggeringWithALongerDurationExtendsTheRemainingTime()
    {
        shake.Trigger(intensity: 1f, duration: 0.2f);
        shake.Tick(0.1f);

        shake.Trigger(intensity: 1f, duration: 1f);
        shake.Tick(0.99f);

        Assert.That(shake.IsShaking, Is.True);
    }

    [Test]
    public void RetriggeringWithAWeakerShorterShakeDoesNotShortenTheStrongerOne()
    {
        shake.Trigger(intensity: 2f, duration: 1f);
        shake.Tick(0.5f);

        shake.Trigger(intensity: 0.5f, duration: 0.1f);
        shake.Tick(0.4f);

        // 원래 걸린 1초짜리가 아직 안 끝났으니 0.5+0.4=0.9초 시점에는 여전히 흔들려야 한다
        Assert.That(shake.IsShaking, Is.True);
    }

    [Test]
    public void NonPositiveTriggerValuesAreIgnored()
    {
        shake.Trigger(intensity: 0f, duration: 1f);
        shake.Trigger(intensity: 1f, duration: 0f);

        Assert.That(shake.IsShaking, Is.False);
    }
}
