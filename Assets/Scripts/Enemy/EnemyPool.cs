using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemyPool : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 50;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    public static EnemyPool Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            NetworkServer.Spawn(enemy);
            enemy.SetActive(false);
            pool.Enqueue(enemy);

        }
    }

    [Server]
    public GameObject SpawnEnemy(Vector3 position)
    {
        if (pool.Count > 0)
        {
            GameObject enemy = pool.Dequeue();
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;
            enemy.SetActive(true);
            EnemyManager.Instance?.RegisterEnemy(enemy.GetComponent<Enemy>());
            // Reset enemy state/stats if necessary
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.InitStatsFromSO();
            }
            enemyScript.ResetState();

            return enemy;
        }
        else
        {
            Debug.LogWarning("EnemyPool exhausted! Consider increasing pool size.");
            return null;
        }
    }

    [Server]
    public void DespawnEnemy(GameObject enemy)
    {
        EnemyManager.Instance?.UnregisterEnemy(enemy.GetComponent<Enemy>());
        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
