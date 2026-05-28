using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float baseSpawnRate = 3f;
    [SerializeField] private int baseSwarmSize = 3;

    [Header("Event Settings")]
    [SerializeField] private float eventDuration = 60f;

    [Header("Event Rewards")]
    [SerializeField] private LootProfileSO eventRewardLootProfile;

    private bool eventRunning;
    private Coroutine eventRoutine;

    [Server]
    public void StartEvent(int difficulty)
    {
        if (eventRunning)
            return;

        eventRoutine = StartCoroutine(EventLoop(Mathf.Max(1, difficulty)));
    }

    [Server]
    private IEnumerator EventLoop(int difficulty)
    {
        eventRunning = true;

        float timer = eventDuration;
        float spawnTimer = 0f;

        if (MapEventState.Instance != null)
            MapEventState.Instance.StartEvent(eventDuration, difficulty);

        Debug.Log($"[EnemySpawner] Event started | difficulty={difficulty}");

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            spawnTimer -= Time.deltaTime;

            if (MapEventState.Instance != null)
                MapEventState.Instance.SetRemainingTime(timer);

            if (spawnTimer <= 0f)
            {
                SpawnWave(difficulty);

                float spawnRate = baseSpawnRate / (1f + difficulty * 0.15f);
                spawnTimer = Mathf.Max(0.5f, spawnRate);
            }

            yield return null;
        }

        eventRunning = false;

        if (MapEventState.Instance != null)
            MapEventState.Instance.EndEvent();

        Debug.Log("[EnemySpawner] Event completed.");

        SpawnRewards(difficulty);
    }

    [Server]
    private void SpawnWave(int difficulty)
    {
        int swarmSize = baseSwarmSize + difficulty;

        for (int i = 0; i < swarmSize; i++)
            SpawnEnemyNearPlayer(difficulty);

        Debug.Log($"[EnemySpawner] SpawnWave | difficulty={difficulty} | count={swarmSize}");
    }

    [Server]
    private void SpawnEnemyNearPlayer(int difficulty)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
            return;

        GameObject player = players[Random.Range(0, players.Length)];
        Vector2 circle = Random.insideUnitCircle * spawnRadius;

        Vector3 rawPos = player.transform.position + new Vector3(circle.x, 0f, circle.y);
        Vector3 spawnPosition = player.transform.position;

        if (NavMesh.SamplePosition(rawPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            spawnPosition = hit.position;

        if (EnemyPool.Instance == null)
        {
            Debug.LogError("[EnemySpawner] EnemyPool.Instance is null.");
            return;
        }
        EnemyPool.Instance.SpawnEnemy(spawnPosition, difficulty);
    }
    [Server]
    private void SpawnRewards(int difficulty)
    {
#if UNITY_SERVER
        if (LootManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] No LootManager found.");
            return;
        }

        if (eventRewardLootProfile == null)
        {
            Debug.LogError("[EnemySpawner] eventRewardLootProfile is missing.");
            return;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
            return;

        Vector3 center = players[0].transform.position;

        int rewardCount = 2 + difficulty;

        for (int i = 0; i < rewardCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 2f;
            offset.y = 0f;

            int seed = Random.Range(0, int.MaxValue);
            int itemLevel = Mathf.Max(1, difficulty);

            LootManager.Instance.GenerateDrops(
                eventRewardLootProfile,
                itemLevel,
                seed,
                center + offset,
                1f,
                1f,
                1f
            );
        }
#endif
    }
}