using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float spawnRadius = 10f;   //radius around player
    [SerializeField] private float spawnRate = 3f;
    [SerializeField] private int swarmSize = 3; // number of enemies to spawn each interval

    [SerializeField] float timer = 0;

    void Update()
    {
        if (!isServer) return; // only Host /Server should handle spawning

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            for (int i = 0; i < swarmSize; i++)
            {
                SpawnEnemyNearPlayer();
            }
            timer = spawnRate;
        }
    }

    void SpawnEnemyNearPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        //Choose a random player
        GameObject player = players[Random.Range(0, players.Length)];

        //Spawn position around the player
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = player.transform.position + new Vector3(circle.x, 0, circle.y);

        // Choose a random enemy prefab
        //GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        //// Instantiate and spawn the enemy on the network
        //GameObject enemyInstance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        //NetworkServer.Spawn(enemyInstance);


        // Using EnemyPool to spawn enemy
        GameObject enemyInstance = EnemyPool.Instance.SpawnEnemy(spawnPos);

    }
}
