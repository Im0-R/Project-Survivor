using System.Collections.Generic;
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
        float dt = Time.deltaTime;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i].isActiveAndEnabled)
            {
                activeEnemies[i].Tick(dt);
            }
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }
}
