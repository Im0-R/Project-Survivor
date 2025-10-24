using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    Transform target;

    public void Enter(Enemy enemy)
    {
        target = enemy.GetClosestPlayer();
        //Get the skinned mesh renderer color to green in children to indicate chase state

        enemy.GetComponentInChildren<SkinnedMeshRenderer>().materials[0].color = Color.green;
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
