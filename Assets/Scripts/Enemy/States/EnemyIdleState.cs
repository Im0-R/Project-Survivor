using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    public void Enter(Enemy enemy) { }
    public void Update(Enemy enemy)
    {
        //after idling, start chasing the closest player
        Transform target = enemy.GetClosestPlayer();
        if (target != null)
        {
            enemy.ChangeState(new EnemyChaseState());
        }
    }
    public void Exit(Enemy enemy) { }
}
