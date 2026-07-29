using NUnit.Framework;
using UnityEngine;

public class Stage1MapDefinitionTests
{
    [TestCase(0f, -5f)]
    [TestCase(0f, 0f)]
    [TestCase(0f, 5f)]
    public void Contains_ReturnsTrueForEachPlayableSection(float x, float y)
    {
        Assert.That(Stage1MapDefinition.Contains(new Vector2(x, y)), Is.True);
    }

    [TestCase(-25f, 0f)]
    [TestCase(25f, 0f)]
    [TestCase(0f, 22.6f)]
    [TestCase(0f, -22.6f)]
    public void Contains_ReturnsFalseBeyondOuterWalls(float x, float y)
    {
        Assert.That(Stage1MapDefinition.Contains(new Vector2(x, y)), Is.False);
    }

    [Test]
    public void OutlineAndContains_UseExactTwoPointFiveScale()
    {
        Assert.That(
            Stage1MapDefinition.Outline[0],
            Is.EqualTo(new Vector2(-22.5f, -20f)));
        Assert.That(
            Stage1MapDefinition.Outline[14],
            Is.EqualTo(new Vector2(3f, 20f)));
        Assert.That(
            Stage1MapDefinition.Contains(new Vector2(0f, -12.5f)),
            Is.True);
        Assert.That(
            Stage1MapDefinition.Contains(new Vector2(0f, 22.6f)),
            Is.False);
    }

    [Test]
    public void Triangles_OnlyReferenceOutlineVertices()
    {
        int vertexCount = Stage1MapDefinition.Outline.Count;

        Assert.That(Stage1MapDefinition.Triangles.Count % 3, Is.Zero);
        foreach (int index in Stage1MapDefinition.Triangles)
        {
            Assert.That(index, Is.InRange(0, vertexCount - 1));
        }
    }
}
