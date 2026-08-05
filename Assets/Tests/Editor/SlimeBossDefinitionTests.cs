using NUnit.Framework;
using UnityEngine;

public sealed class SlimeBossDefinitionTests
{
    [Test]
    public void NewDefinition_UsesApprovedDefaults()
    {
        var definition = ScriptableObject.CreateInstance<SlimeBossDefinition>();

        Assert.That(definition.MaxHealth, Is.EqualTo(100));
        Assert.That(definition.MoveSpeed, Is.EqualTo(3f));
        Assert.That(definition.PatternInterval, Is.EqualTo(2f));
        Assert.That(definition.ContactDamage, Is.EqualTo(4));
        Assert.That(definition.ProjectileDamage, Is.EqualTo(3));
        Assert.That(definition.MarkerVisualHeight, Is.EqualTo(2f));

        Object.DestroyImmediate(definition);
    }
}
