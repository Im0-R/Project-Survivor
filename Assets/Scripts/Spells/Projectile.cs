using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private NetworkEntity owner;
    private Vector3 direction;

    private int pierceRemaining;

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
        target = targetTransform;
        damage = dmg;
        speed = spd;
        pierceRemaining = pierce;

        if (forcedDirection.HasValue)
            direction = forcedDirection.Value.normalized;
        else
            direction = target != null ? (target.position - transform.position).normalized : transform.forward;

        direction.y = 0f;

        if (direction != Vector3.zero)
            transform.forward = direction;

        transform.localScale *= scale;

        if (isServer)
            Invoke(nameof(DespawnSelf), lifeTime);
    }

    private void Update()
    {
        if (!isServer) return;

        transform.position += direction * speed * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0f)
            DespawnSelf();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        NetworkEntity otherNetEntity = other.GetComponent<NetworkEntity>();

        if (otherNetEntity == null || otherNetEntity == owner)
            return;

        if (otherNetEntity is PlayerEntity && owner is PlayerEntity) return;
        if (otherNetEntity is EnemyEntity && owner is EnemyEntity) return;

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
        NetworkIdentity ni = GetComponent<NetworkIdentity>();

        if (ni != null && ni.netId != 0)
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }
}