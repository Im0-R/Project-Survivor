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
        enemy.SetActive(true);

        // Snap NavMesh
        Vector3 finalPos = position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            finalPos = hit.position;

        // Warp / set transform
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
        enemy.transform.rotation = Quaternion.identity;

        // Spawn réseau (si pas déjà spawn)
        NetworkIdentity ni = enemy.GetComponent<NetworkIdentity>();
        if (ni != null && ni.netId == 0)
            NetworkServer.Spawn(enemy);

        // Init serveur
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.ResetForSpawn();
            EnemyManager.Instance?.RegisterEnemy(enemyScript);
        }

        Debug.Log($"[EnemyPool] Spawned {enemy.name} at {finalPos} | scene={enemy.scene.name}");
        return enemy;
    }


    [Server]
    public void DespawnEnemy(GameObject enemy)
    {
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.ResetForDespawn();
            EnemyManager.Instance?.UnregisterEnemy(enemyScript);
        }

        NetworkIdentity ni = enemy.GetComponent<NetworkIdentity>();
        if (ni != null && ni.netId != 0)
        {
            enemyScript?.RpcSetActive(false);
            NetworkServer.UnSpawn(enemy);
        }

        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
