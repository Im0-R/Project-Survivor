using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    Transform target;
    float attackCooldown = 3.5f;
    float timer = 0;

    public EnemyAttackState(Transform player)
    {
        target = player;
    }

    public void Enter(Enemy enemy) { timer = 0; }

    public void Update(Enemy enemy)
    {
        if (target == null)
        {
            enemy.ChangeState(new EnemyChaseState());
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = attackCooldown;
            enemy.humanoidAnimator.StartAttackAnim();
        }

        float dist = Vector3.Distance(enemy.transform.position, target.position);
        if (dist > enemy.attackDamage)
        {
            enemy.ChangeState(new EnemyChaseState());
        }
    }

    public void Exit(Enemy enemy) { }
}
