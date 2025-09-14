using UnityEngine;

/// <summary>
/// Put the camera on top of the player.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("Offset relatif au joueur. Ex: (0, 15, -6) pour vue top/iso.")]
    public Vector3 offset = new Vector3(0f, 18f, -6f);

    [Tooltip("Vitesse de lissage (plus grand = plus réactif).")]
    public float smoothSpeed = 8f;

    [Tooltip("Si vrai, la caméra regarde toujours le joueur.")]
    public bool lookAtTarget = true;

    [Tooltip("Le transform du joueur à suivre.")]
    public Transform target;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public void ClearTarget()
    {
        target = null;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        if (lookAtTarget)
        {
            // Regarde le point du joueur (on conserve l'angle top-down en ne changeant pas l'up)
            Vector3 lookPoint = target.position;
            transform.LookAt(lookPoint);
        }
    }
}
