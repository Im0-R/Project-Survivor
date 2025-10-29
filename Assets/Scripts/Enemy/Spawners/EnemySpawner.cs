using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float spawnRate = 3f;
    [SerializeField] private int swarmSize = 3;

    float timer = 0;

    void Update()
    {
        if (!isServer) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            for (int i = 0; i < swarmSize; i++)
                SpawnEnemyNearPlayer();

            timer = spawnRate;
        }
    }

    void SpawnEnemyNearPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        GameObject player = players[Random.Range(0, players.Length)];
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = player.transform.position + new Vector3(circle.x, 0, circle.y);

        EnemyPool.Instance.SpawnEnemy(pos); // this will Spawn() if needed
    }
}
