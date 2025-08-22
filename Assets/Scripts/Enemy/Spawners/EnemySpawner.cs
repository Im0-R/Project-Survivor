using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject[] enemyPrefabs; // tes prefabs (Zombie, etc.)
    [SerializeField] private float spawnRadius = 10f;   // rayon autour du joueur
    [SerializeField] private int maxEnemies = 10;       // max sur la map
    [SerializeField] private float spawnInterval = 3f;  // temps entre les spawns

    private float timer = 0f;
    private int currentEnemyCount = 0;
    void Start()
    {
        timer = 3f;
    }
    void Update()
    {
        if (!IsServer) return; // seulement le Host spawn les ennemis

        timer -= Time.deltaTime;
        if (timer <= 0f && currentEnemyCount < maxEnemies)
        {
            SpawnEnemyNearPlayer();
            timer = spawnInterval;
        }
    }

    void SpawnEnemyNearPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        // Choisir un joueur aléatoire
        GameObject player = players[Random.Range(0, players.Length)];

        // Générer une position aléatoire autour du joueur
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = player.transform.position + new Vector3(circle.x, 0, circle.y);

        // Choisir un prefab aléatoire
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Instancier et spawn sur le réseau
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemyInstance.GetComponent<NetworkObject>().Spawn();
        currentEnemyCount++;
    }
}
