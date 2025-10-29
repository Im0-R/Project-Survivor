// EnemyPool.cs
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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        // IMPORTANT: do NOT NetworkServer.Spawn here
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            // enemyPrefab MUST already have a NetworkIdentity on the prefab
            enemy.SetActive(false);
            pool.Enqueue(enemy);
        }
    }

    [Server]
    public GameObject SpawnEnemy(Vector3 position)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("EnemyPool exhausted! Consider increasing pool size.");
            return null;
        }

        GameObject enemy = pool.Dequeue();

        // reset transform & enable
        enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
        enemy.SetActive(true);

        NetworkIdentity ni = enemy.GetComponent<NetworkIdentity>();
        // If not spawned yet (or previously UnSpawned), netId will be 0
        if (ni.netId == 0)
            NetworkServer.Spawn(enemy);

        // reset gameplay state
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.InitStatsFromSO();
            enemyScript.ResetState();
            EnemyManager.Instance?.RegisterEnemy(enemyScript);
        }

        return enemy;
    }

    [Server]
    public void DespawnEnemy(GameObject enemy)
    {
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
            EnemyManager.Instance?.UnregisterEnemy(enemyScript);

        NetworkIdentity ni = enemy.GetComponent<NetworkIdentity>();
        // If currently spawned (netId != 0), unspawn so clients despawn it
        if (ni != null && ni.netId != 0)
            NetworkServer.UnSpawn(enemy);

        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
