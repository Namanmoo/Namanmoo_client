using NUnit.Framework;
using UnityEngine;

public sealed class PlayerDashAfterimageTests
{
    [TestCase(0f, 1f)]
    [TestCase(0.5f, 0.5f)]
    [TestCase(1f, 0f)]
    [TestCase(-1f, 1f)]
    [TestCase(2f, 0f)]
    public void EvaluateColor_FadesAlphaLinearlyAndClampsAge(float age, float alpha)
    {
        Color result = PlayerDashAfterimage.EvaluateColor(
            new Color(0.2f, 0.4f, 0.6f, 1f), age);

        Assert.That(result.r, Is.EqualTo(0.2f));
        Assert.That(result.g, Is.EqualTo(0.4f));
        Assert.That(result.b, Is.EqualTo(0.6f));
        Assert.That(result.a, Is.EqualTo(alpha).Within(0.0001f));
    }
}
