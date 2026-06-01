using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private readonly List<Enemy> activeEnemies = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!NetworkServer.active)
            return;

        if (ServerTimeManager.IsPaused)
            return;

        float dt = Time.deltaTime;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.isActiveAndEnabled)
                enemy.Tick(dt);
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Remove(enemy);
    }
}