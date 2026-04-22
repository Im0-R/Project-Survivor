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
        if (!NetworkServer.active) return;

        if (ServerTimeManager.instance != null && ServerTimeManager.instance.isPaused)
            return;

        float dt = Time.deltaTime;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && activeEnemies[i].isActiveAndEnabled)
                activeEnemies[i].Tick(dt);
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