using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HumanoidAnimator : MonoBehaviour
{
    Enemy enemy;
    Animator animator;
    void Start()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("Enemy component not found in parent.");
            return;
        }
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        if (animator == null || enemy.GetAgent() == null) return;
            animator.SetFloat("Speed", enemy.GetAgent().velocity.magnitude);
            animator.SetFloat("Health", enemy.currentHealth);
    }
    public void EnableAttackHitbox()
    {
        enemy.hitboxHit.EnableHitbox();
    }
    public void DisableAttackHitbox()
    {
        enemy.hitboxHit.DisableHitbox();
    }
    public void StartAttackAnim()
    {
        animator.SetTrigger("Attack");
    }
}
