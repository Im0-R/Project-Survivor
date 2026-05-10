using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private MapConfigSO[] maps;

    private GameObject currentEnvironment;
    private MapConfigSO currentMap;

    public IEnumerator Generate(string mapId, int seed)
    {

        Debug.Log(
    $"[MapGenerator] Generate START | " +
    $"mapId={mapId} | " +
    $"seed={seed} | " +
    $"isServer={Application.isBatchMode}"
);
        if (string.IsNullOrWhiteSpace(mapId))
        {
            Debug.LogError("[MapGenerator] Generate called with empty mapId");
            yield break;
        }

        currentMap = FindMap(mapId);

        if (currentMap == null)
        {
            Debug.LogError($"[MapGenerator] MapConfig not found for mapId={mapId}");
            yield break;
        }

        if (currentMap.biome == null || currentMap.biome.environmentPrefab == null)
        {
            Debug.LogError($"[MapGenerator] Invalid biome or environmentPrefab for mapId={mapId}");
            yield break;
        }

        if (currentEnvironment != null)
            Destroy(currentEnvironment);
        Debug.Log(
    $"[MapGenerator] Instantiating environment prefab: " +
    $"{currentMap.biome.environmentPrefab.name}"
);
        currentEnvironment = Instantiate(
            currentMap.biome.environmentPrefab,
            Vector3.zero,
            Quaternion.identity
        );
        Debug.Log(
    $"[MapGenerator] Environment instantiated: " +
    $"{currentEnvironment.name}"
);
        RenderSettings.ambientLight = currentMap.biome.ambientColor;

#if UNITY_SERVER
        NavMeshSurface navMeshSurface = currentEnvironment.GetComponentInChildren<NavMeshSurface>();

        if (navMeshSurface == null)
        {
            Debug.LogError("[MapGenerator] No NavMeshSurface found in environment prefab");
            yield break;
        }

        navMeshSurface.BuildNavMesh();
#endif

        Debug.Log($"[MapGenerator] Map generated | mapId={mapId} | seed={seed}");
    }

    public Transform GetPlayerSpawnPoint()
    {
        if (currentEnvironment == null)
            return null;

        Transform spawn = currentEnvironment.transform.Find("SpawnPoints/PlayerSpawn");

        if (spawn == null)
        {
            Debug.LogWarning("[MapGenerator] SpawnPoints/PlayerSpawn not found");
            return currentEnvironment.transform;
        }

        return spawn;
    }

    private MapConfigSO FindMap(string mapId)
    {
        foreach (MapConfigSO map in maps)
        {
            if (map != null && map.mapId == mapId)
                return map;
        }

        return null;
    }
}