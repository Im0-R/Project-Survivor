using UnityEngine;
using UnityEngine.SceneManagement;

public class TestDebugLog : MonoBehaviour
{
    void Start()
    {
        if (Application.isBatchMode)
            Debug.Log("[SERVER] Running headless server");

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            Debug.Log("[SERVER] Null graphics device confirmed");

        Debug.Log("Build type: " + (Application.isEditor ? "Editor" : "Build"));

        Debug.Log("[SERVER] Loaded Scene = " + SceneManager.GetActiveScene().name);
    }
}
