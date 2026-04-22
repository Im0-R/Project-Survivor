using Mirror;
using UnityEngine;

public class DebugSceneIds : MonoBehaviour
{
    private void Start()
    {
        var ids = FindObjectsByType<NetworkIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log($"[DEBUG] NetworkIdentity count = {ids.Length}");

        foreach (var ni in ids)
        {
            Debug.Log(
                $"[DEBUG] name={ni.name} scene={ni.gameObject.scene.name} " +
                $"sceneId={ni.sceneId:X} assetId={ni.assetId}"
            );
        }
    }
}