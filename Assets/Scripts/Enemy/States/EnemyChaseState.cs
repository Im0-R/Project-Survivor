using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private Transform target;
    private float repathTimer;

    public void Enter(Enemy enemy)
    {
        target = enemy.GetClosestPlayer();
        repathTimer = 0f;

        var smr = enemy.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && smr.materials != null && smr.materials.Length > 0)
            smr.materials[0].color = Color.green;

        var agent = enemy.GetAgent();
        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    public void Update(Enemy enemy)
    {
        if (target == null) target = enemy.GetClosestPlayer();
        if (target == null) return;

        var agent = enemy.GetAgent();
        if (agent == null) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            repathTimer = 0.2f;
        }

        float dist = Vector3.Distance(enemy.transform.position, target.position);
        if (dist < enemy.AttackRange)
            enemy.ChangeState(new EnemyAttackState(target));
    }

    public void Exit(Enemy enemy) { }
}