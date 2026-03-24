using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void SetSpeed(float speed)
    {
        if (animator != null)
            animator.SetFloat("Speed", speed);
    }

    public void TriggerDeath()
    {
        if (animator != null)
            animator.SetBool("Death", true);
    }
}
