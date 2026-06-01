using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private float speed;
    private float damage;
    private NetworkEntity owner;
    private Vector3 direction;

    private int pierceRemaining;
    private bool initialized;

    [SerializeField] private float lifeTime = 3f;

    public void Initialize(
        NetworkEntity ownerEntity,
        Transform targetTransform,
        float dmg,
        float spd = 10f,
        float scale = 1f,
        int pierce = 0,
        Vector3? forcedDirection = null)
    {
        owner = ownerEntity;
        damage = dmg;
        speed = spd;
        pierceRemaining = Mathf.Max(0, pierce);

        if (forcedDirection.HasValue)
        {
            direction = forcedDirection.Value;
        }
        else if (targetTransform != null)
        {
            direction = targetTransform.position - transform.position;
        }
        else
        {
            direction = transform.forward;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        direction.Normalize();

        transform.rotation = Quaternion.LookRotation(direction);
        transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        initialized = true;
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (ServerTimeManager.IsPaused)
            return;

        if (!initialized)
            return;

        transform.position += direction * speed * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0f)
            DespawnSelf();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (ServerTimeManager.IsPaused)
            return;

        if (!initialized)
            return;

        NetworkEntity otherNetEntity = other.GetComponent<NetworkEntity>();

        if (otherNetEntity == null || otherNetEntity == owner)
            return;

        if (otherNetEntity is PlayerEntity && owner is PlayerEntity)
            return;

        if (otherNetEntity is EnemyEntity && owner is EnemyEntity)
            return;

        otherNetEntity.ApplyDamageServer(damage);

        if (pierceRemaining > 0)
        {
            pierceRemaining--;
            return;
        }

        DespawnSelf();
    }

    [Server]
    public void DespawnSelf()
    {
        if (gameObject == null)
            return;

        NetworkIdentity ni = GetComponent<NetworkIdentity>();

        if (ni != null && ni.netId != 0)
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }
}