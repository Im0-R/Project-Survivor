#if UNITY_SERVER
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private MapConfigSO[] maps;

    private GameObject currentEnvironment;
    private MapConfigSO currentMap;

    public IEnumerator Generate()
    {
        string mapId = InstanceBootStrap.MapIdArg;

        if (string.IsNullOrWhiteSpace(mapId))
        {
            Debug.LogError("[MapGenerator] No mapId provided for MapInstance");
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

        currentEnvironment = Instantiate(currentMap.biome.environmentPrefab, Vector3.zero, Quaternion.identity);

        RenderSettings.ambientLight = currentMap.biome.ambientColor;

        NavMeshSurface navMeshSurface = currentEnvironment.GetComponent<NavMeshSurface>();

        if (navMeshSurface == null)
            navMeshSurface = currentEnvironment.GetComponentInChildren<NavMeshSurface>();

        if (navMeshSurface == null)
        {
            Debug.LogError("[MapGenerator] No NavMeshSurface found in environment prefab");
            yield break;
        }

        navMeshSurface.BuildNavMesh();

        yield return null;

        Debug.Log($"[MapGenerator] Map generated | mapId={mapId}");
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
#endif