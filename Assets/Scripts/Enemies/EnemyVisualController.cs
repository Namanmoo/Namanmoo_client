using UnityEngine;

public sealed class EnemyVisualController : MonoBehaviour
{
    private const string AttackTrigger = "Attack";

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public void Configure(Sprite sprite, RuntimeAnimatorController animatorController)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;

        if (animatorController == null)
        {
            animator = null;
            return;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = animatorController;
    }

    public void PlayAttack()
    {
        if (animator != null && HasAttackTrigger(animator))
        {
            animator.SetTrigger(AttackTrigger);
        }
    }

    private static bool HasAttackTrigger(Animator targetAnimator)
    {
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == AttackTrigger && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }
}
