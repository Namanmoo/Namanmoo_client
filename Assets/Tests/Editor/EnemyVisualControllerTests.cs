using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class EnemyVisualControllerTests
{
    [Test]
    public void Configure_AssignsSpriteToItsRenderer()
    {
        GameObject visualObject = new GameObject("Enemy Visual");
        Sprite sprite = CreateSprite(Color.red);

        try
        {
            EnemyVisualController visual = visualObject.AddComponent<EnemyVisualController>();

            visual.Configure(sprite, null);

            Assert.That(visualObject.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(sprite));
        }
        finally
        {
            Object.DestroyImmediate(visualObject);
            DestroySprite(sprite);
        }
    }

    [Test]
    public void PlayAttack_WithoutAnimatorController_IsSafe()
    {
        GameObject visualObject = new GameObject("Enemy Visual");

        try
        {
            EnemyVisualController visual = visualObject.AddComponent<EnemyVisualController>();
            visual.Configure(null, null);

            Assert.DoesNotThrow(() => visual.PlayAttack());
            Assert.That(visualObject.GetComponent<Animator>(), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(visualObject);
        }
    }

    [Test]
    public void PlayAttack_WithAttackTrigger_TransitionsAnimator()
    {
        const string controllerPath = "Assets/__EnemyVisualControllerTests.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle");
        AnimatorState attack = stateMachine.AddState("AttackState");
        AnimatorStateTransition transition = idle.AddTransition(attack);
        transition.duration = 0f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        transition.hasExitTime = false;
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        GameObject visualObject = new GameObject("Enemy Visual");

        try
        {
            EnemyVisualController visual = visualObject.AddComponent<EnemyVisualController>();
            visual.Configure(null, controller);
            Animator animator = visualObject.GetComponent<Animator>();
            animator.Play("Idle");
            animator.Update(0f);

            visual.PlayAttack();
            animator.Update(0f);

            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("AttackState"), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(visualObject);
            AssetDatabase.DeleteAsset(controllerPath);
        }
    }

    private static Sprite CreateSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static void DestroySprite(Sprite sprite)
    {
        Texture2D texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
