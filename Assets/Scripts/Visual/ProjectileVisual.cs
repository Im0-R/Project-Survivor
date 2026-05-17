using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    private int projectileId;
    private ProjectileMotionType motionType;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 direction;

    private float speed;
    private float lifetime;
    private float arcHeight;
    private float spawnTime;

    public void Init(
        int id,
        ProjectileMotionType motion,
        Vector3 start,
        Vector3 target,
        Vector3 dir,
        float moveSpeed,
        float duration,
        float height)
    {
        projectileId = id;
        motionType = motion;

        startPosition = start;
        targetPosition = target;
        direction = dir.normalized;

        speed = moveSpeed;
        lifetime = duration;
        arcHeight = height;
        spawnTime = Time.time;

        transform.position = startPosition;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifetime);

        switch (motionType)
        {
            case ProjectileMotionType.Straight:
                transform.position += direction * speed * Time.deltaTime;
                break;

            case ProjectileMotionType.TargetPosition:
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                break;

            case ProjectileMotionType.Arc:
                Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = pos;
                break;
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ClientSpellVisualManager.Instance != null)
            ClientSpellVisualManager.Instance.UnregisterProjectile(projectileId);
    }
}