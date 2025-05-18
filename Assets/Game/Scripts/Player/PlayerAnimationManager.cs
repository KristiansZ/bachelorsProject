using UnityEngine;
using System.Collections;

public class PlayerAnimationManager : MonoBehaviour
{
    private Animator animator;
    private static readonly int DirectionalAttackTrigger = Animator.StringToHash("directionalAttack");
    private static readonly int TargetedAttackTrigger = Animator.StringToHash("targetedAttack");
    private static readonly int IsRunningParam = Animator.StringToHash("isMoving");
    private static readonly int AttackSpeedParam = Animator.StringToHash("attackSpeed");

    private PlayerController playerController;
    private bool isAttacking = false;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found on GameObject with PlayerAnimationManager!");
        }
        
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("No PlayerController component found on GameObject with PlayerAnimationManager!");
        }
    }

    public void UpdateMovementAnimation(bool isMoving)
    {
        if (animator != null && !isAttacking)
        {
            animator.SetBool(IsRunningParam, isMoving);
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public void PlayDirectionalAttack(float attackSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat(AttackSpeedParam, attackSpeed);
            animator.SetTrigger(DirectionalAttackTrigger);

            float animationDuration = GetAnimationLength("PlayerDirected") / attackSpeed;

            StartCoroutine(AttackStateCoroutine(animationDuration));
        }
    }

    public void PlayTargetedAttack(float attackSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat(AttackSpeedParam, attackSpeed);
            animator.SetTrigger(TargetedAttackTrigger);

            float animationDuration = GetAnimationLength("PlayerTargeted") / attackSpeed;

            StartCoroutine(AttackStateCoroutine(animationDuration));
        }
    }
    
    private IEnumerator AttackStateCoroutine(float duration)
    {
        isAttacking = true;
        
        if (playerController != null)
        {
            playerController.LockMovement(true);
        }
        
        yield return new WaitForSeconds(duration);
        
        isAttacking = false;
        
        if (playerController != null)
        {
            playerController.LockMovement(false);
        }
    }
    
    public float GetAnimationLength(string clipName)
    {
        if (animator == null) return 1.0f;
        
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name.Contains(clipName))
            {
                return clip.length;
            }
        }
        
        Debug.LogWarning($"Animation clip containing '{clipName}' not found! Using default duration of 1.0s");
        return 1.0f;
    }
}