using Mirror;
using UnityEngine;

public class SlashBehaviour : NetworkBehaviour
{
    private float damage;
    private NetworkEntity owner;
    private float radius;
    private float duration;

    [SerializeField] private float lifeTime = 0.3f; // durée du slash
    private bool hasDespawned = false;

    private float elapsed = 0f;
    private Transform player;

    // orientation figée
    private Vector3 initialRight;
    private Vector3 initialForward;

    private TrailRenderer trailRenderer;

    public void Initialize(NetworkEntity ownerEntity, float dmg, float dur, float rad)
    {
        owner = ownerEntity;
        damage = dmg;
        duration = dur;
        radius = rad;
        player = ownerEntity.transform;

        // On capture l'orientation du joueur au moment du cast
        initialRight = player.right;
        initialForward = player.forward;

        trailRenderer = GetComponent<TrailRenderer>();

        trailRenderer.widthMultiplier = radius / 4f;

        if (isServer) // seul le serveur gère le despawn
        {
            Invoke(nameof(DespawnSelf), lifeTime);
        }
    }

    void Update()
    {
        if (!isServer) return;
        if (player == null) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        float angle = Mathf.Lerp(0, 180f, t);

        

        // Starting Left Side
        Vector3 baseDir = -initialRight;

        //Rotation around Y axis
        Vector3 rotatedDir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

        // offset based on radius
        Vector3 offset = rotatedDir.normalized * radius;
        transform.position = player.position + offset;


        transform.rotation = Quaternion.LookRotation(rotatedDir);

        //Only enable trail once it starts moving
        var trail = GetComponent<TrailRenderer>();
        if (trail != null && !trail.enabled)
        {
            trail.enabled = true;
        }

        if (elapsed >= duration)
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

            // Appliquer les dégâts
            otherNetEntity.CmdApplyDamage(damage);
        }
    }

    private void DespawnSelf()
    {
        if (hasDespawned) return;
        hasDespawned = true;

        if (isServer)
        {
            // Détruit l'objet sur le serveur ET tous les clients
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            // Si on n'est pas sur le serveur (ex: client local), on le détruit juste localement
            Destroy(gameObject);
        }
    }

}
