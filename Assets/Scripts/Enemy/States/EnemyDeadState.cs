using UnityEngine;

public class EnemyDeadState : IEnemyState
{
    public void Enter(Enemy enemy)
    {
        Debug.Log("L’ennemi est mort !");
        GameObject.Destroy(enemy.gameObject);
    }

    public void Update(Enemy enemy) { }
    public void Exit(Enemy enemy) { }
}
