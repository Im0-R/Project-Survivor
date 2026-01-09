using UnityEngine;

/// <summary>
/// Put the camera on top of the player.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public static Camera LocalCamera { get; private set; }

    [Tooltip("Offset relatif au joueur. Ex: (0, 15, -6) pour vue top/iso.")]
    public Vector3 offset = new Vector3(0f, 18f, -6f);

    [Tooltip("Vitesse de lissage (plus grand = plus réactif).")]
    public float smoothSpeed = 8f;

    [Tooltip("Si vrai, la caméra regarde toujours le joueur.")]
    public bool lookAtTarget = true;

    [Tooltip("Le transform du joueur à suivre.")]
    public Transform target;

    public bool HasTarget = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
        LocalCamera = GetComponent<Camera>();
    }

    public void SetTarget(Transform t)
    {
        target = t;
        HasTarget = true;
    }

    public void ClearTarget()
    {
        target = null;
        HasTarget = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);

        if (lookAtTarget)
        {
            // Regarde le point du joueur (on conserve l'angle top-down en ne changeant pas l'up)
            Vector3 lookPoint = target.position;
            transform.LookAt(lookPoint);
        }
    }
}
