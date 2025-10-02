using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HumanoidAnimator : MonoBehaviour
{
    Enemy enemy;
    Animator animator;
    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("Enemy component not found in parent.");
            return;
        }
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        animator.SetFloat("Speed", enemy.GetAgent().velocity.magnitude);
        animator.SetFloat("Health", enemy.currentHealth.Value);
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
