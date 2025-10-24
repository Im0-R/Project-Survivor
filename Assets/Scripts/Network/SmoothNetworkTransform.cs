using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class SmoothNetworkTransform : NetworkBehaviour
{
    [Header("Sync Settings")]
    [Tooltip("How often (in seconds) to send updates from the server to clients.")]
    [SerializeField] private float sendInterval = 0.1f;

    [Tooltip("Speed of position interpolation on clients.")]
    [SerializeField] private float positionLerpSpeed = 10f;

    [Tooltip("Speed of rotation interpolation on clients.")]
    [SerializeField] private float rotationLerpSpeed = 10f;

    [Header("Debug")]
    [SerializeField, ReadOnly] private Vector3 targetPosition;
    [SerializeField, ReadOnly] private Quaternion targetRotation;

    private float lastSendTime;

    // ---------------------------------------------------------
    // SERVER → Envoi périodique de la position
    // ---------------------------------------------------------
    void Update()
    {
        if (isServer)
        {
            if (Time.time - lastSendTime >= sendInterval)
            {
                RpcSyncTransform(transform.position, transform.rotation);
                lastSendTime = Time.time;
            }
        }
        else if (isClient)
        {
            // Interpolation lissée côté client
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }

    // ---------------------------------------------------------
    // CLIENT → Réception des updates
    // ---------------------------------------------------------
    [ClientRpc(channel = Channels.Unreliable)]
    void RpcSyncTransform(Vector3 pos, Quaternion rot)
    {
        // Ne rien faire côté serveur ou host
        if (isServer) return;

        targetPosition = pos;
        targetRotation = rot;
    }

    // ---------------------------------------------------------
    // (Optionnel) Reset lors du spawn
    // ---------------------------------------------------------
    public override void OnStartClient()
    {
        base.OnStartClient();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }
}
