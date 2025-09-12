using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private NetworkEntity owner;
    private Vector3 direction;

    [SerializeField] private float lifeTime = 3f;

    public void Initialize(NetworkEntity ownerEntity, Transform targetTransform, float dmg, float spd = 10f)
    {
        owner = ownerEntity;
        target = targetTransform;
        damage = dmg;
        speed = spd;
        direction = (target != null) ? (target.position - transform.position).normalized : transform.forward;

            Invoke(nameof(DespawnSelf), lifeTime);
    }

    void Update()
    {
        if (!IsServer) return;

        if (target != null)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Projectile hit: " + other.name);
        if (!IsServer) return;

        var otherNetEntity = other.GetComponent<NetworkEntity>();

        if (otherNetEntity != null && otherNetEntity != owner)
        {
            //Check for friendly fire
            if (otherNetEntity is PlayerEntity && owner is PlayerEntity) return;
            if (otherNetEntity is EnemyEntity && owner is EnemyEntity) return;


            otherNetEntity.ApplyDamageServerRpc(damage);
            DespawnSelf();
            Debug.Log("Projectile applied damage and despawned.");
        }
    }

    private void DespawnSelf()
    {
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            // Force la destruction réseau et locale
            netObj.Despawn(destroy: true);
        }
        else
        {
            // Destruction locale si pas de NetworkObject (sécurité)
            Destroy(gameObject);
        }
    }

}
