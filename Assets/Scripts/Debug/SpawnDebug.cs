using Mirror;
using UnityEngine;

public static class SpawnDebug
{
    public static void LogSpawn(GameObject obj, string source)
    {
        if (obj == null)
        {
            Debug.LogError($"[SPAWN DEBUG] NULL object | source={source}");
            return;
        }

        NetworkIdentity ni = obj.GetComponent<NetworkIdentity>();

        if (ni == null)
        {
            Debug.LogError($"[SPAWN DEBUG] {obj.name} has NO NetworkIdentity | source={source}");
            return;
        }

        Debug.Log(
            $"[SPAWN DEBUG] " +
            $"source={source} | " +
            $"name={obj.name} | " +
            $"assetId={ni.assetId} | " +
            $"sceneId={ni.sceneId}"
        );
    }
}