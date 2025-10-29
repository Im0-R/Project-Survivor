using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private NetworkEntity owner;
    private Vector3 direction;

    [SerializeField] private float lifeTime = 3f;
    private bool hasDespawned = false;

    public void Initialize(NetworkEntity ownerEntity, Transform targetTransform, float dmg, float spd = 10f, float scale = 1f)
    {
        owner = ownerEntity;
        target = targetTransform;
        damage = dmg;
        speed = spd;
        direction = (target != null) ? (target.position - transform.position).normalized : transform.forward;
        //projectile can't go down or up
        direction.y = 0;
        transform.forward = direction;

        transform.localScale = transform.localScale * scale;

        if (isServer) // important : seul le serveur gère le despawn
        {
            Invoke(nameof(DespawnSelf), lifeTime);
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (target != null)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            DespawnSelf();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var otherNetEntity = other.GetComponent<NetworkEntity>();
        if (otherNetEntity != null && otherNetEntity != owner)
        {
            // Check friendly fire 
            if (otherNetEntity is PlayerEntity && owner is PlayerEntity) return;
            if (otherNetEntity is EnemyEntity && owner is EnemyEntity) return;

            otherNetEntity.CmdApplyDamage(damage);
            DespawnSelf();
        }
    }
    [Server]
    public void DespawnSelf()
    {
        EnemyPool.Instance?.DespawnEnemy(gameObject);
    }
}
