using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    Transform target;

    public void Enter(Enemy enemy)
    {
        target = enemy.GetClosestPlayer();
    }

    public void Update(Enemy enemy)
    {
        
        if (target == null) target = enemy.GetClosestPlayer();

        if (target == null) return;

        enemy.GetAgent().SetDestination(target.position);

        float dist = Vector3.Distance(enemy.transform.position, target.position);
        if (dist < enemy.attackRange)
        {
            enemy.ChangeState(new EnemyAttackState(target));
        }
    }

    public void Exit(Enemy enemy) { }
}
