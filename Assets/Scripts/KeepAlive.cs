#if !UNITY_CLIENT
using UnityEngine;

public class KeepAlive : MonoBehaviour
{
    void Update()
    {
        //Maintain the game object alive
        // This is useful for keeping the server running
    }
}
#endif
