using Mirror;
using UnityEngine;

public class EnemyDeadState : IEnemyState
{
    public void Enter(Enemy enemy)
    {
        enemy.RequestDeathServer();
    }

    public void Update(Enemy enemy) { }
    public void Exit(Enemy enemy) { }
}