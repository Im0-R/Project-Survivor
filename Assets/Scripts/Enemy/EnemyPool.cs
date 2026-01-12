using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);

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

        // Activate enemy before setting position
        enemy.SetActive(true);

        //Snap to NavMesh nearest position
        Vector3 finalPos = position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            finalPos = hit.position;

        //Warp the agent if possible
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(finalPos);
            agent.isStopped = false;
            agent.ResetPath();
        }
        else
        {
            enemy.transform.SetPositionAndRotation(finalPos, Quaternion.identity);
        }

        //Reset rotation
        enemy.transform.rotation = Quaternion.identity;

        NetworkIdentity ni = enemy.GetComponent<NetworkIdentity>();
        if (ni != null && ni.netId == 0)
            NetworkServer.Spawn(enemy);


        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.InitStatsFromSO();

            // Start directly in Chase
            enemyScript.ChangeState(new EnemyChaseState());

            // Helpful debug
            Debug.Log($"[EnemyPool] Spawned {enemy.name} at {finalPos} | " +
                      $"scene={enemy.gameObject.scene.name} | " +
                      $"agentOnNavMesh={(agent != null ? agent.isOnNavMesh.ToString() : "no-agent")}");

            EnemyManager.Instance?.RegisterEnemy(enemyScript);
        }
        else
        {
            Debug.LogWarning($"[EnemyPool] Spawned enemy has no Enemy script: {enemy.name}");
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

        // Only UnSpawn if it was spawned
        if (ni != null && ni.netId != 0)
        {
            NetworkServer.UnSpawn(enemy);
            Debug.Log($"[EnemyPool] UnSpawned {enemy.name}");
        }

        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
