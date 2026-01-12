using Mirror;
using UnityEngine;

public class EnemyDeadState : IEnemyState
{
    public void Enter(Enemy enemy)
    {
        //Disable from poolPool
        EnemyPool.Instance?.DespawnEnemy(enemy.gameObject);
    }

    public void Update(Enemy enemy) { }
    public void Exit(Enemy enemy) { }
}