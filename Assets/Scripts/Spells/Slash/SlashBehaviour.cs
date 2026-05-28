using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SlashBehaviour : NetworkBehaviour
{
    private float damage;
    private NetworkEntity owner;
    private float radius;
    private float duration;

    [SerializeField] private float lifeTime = 0.3f;

    private bool hasDespawned;
    private float elapsed;

    private Transform player;
    private Vector3 initialRight;

    private readonly HashSet<NetworkEntity> alreadyHit = new();

    private TrailRenderer trailRenderer;

    public void Initialize(NetworkEntity ownerEntity, float dmg, float dur, float rad)
    {
        owner = ownerEntity;
        damage = dmg;
        duration = Mathf.Max(0.05f, dur);
        radius = rad;

        if (ownerEntity != null)
        {
            player = ownerEntity.transform;
            initialRight = player.right;
        }
        else
        {
            initialRight = transform.right;
        }

        trailRenderer = GetComponent<TrailRenderer>();

        if (trailRenderer != null)
        {
            trailRenderer.widthMultiplier = radius / 4f;
            trailRenderer.enabled = true;
        }

        SetSlashPosition(0f);

        if (isServer)
            Invoke(nameof(DespawnSelf), lifeTime);
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (player == null)
        {
            DespawnSelf();
            return;
        }

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float angle = Mathf.Lerp(0f, 180f, t);

        SetSlashPosition(angle);

        if (elapsed >= duration)
            DespawnSelf();
    }

    private void SetSlashPosition(float angle)
    {
        Vector3 baseDir = -initialRight;
        Vector3 rotatedDir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

        Vector3 center = player != null ? player.position : transform.position;
        Vector3 offset = rotatedDir.normalized * radius;

        transform.position = center + offset;

        if (rotatedDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(rotatedDir);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        NetworkEntity otherNetEntity = other.GetComponentInParent<NetworkEntity>();

        if (otherNetEntity == null)
            return;

        if (otherNetEntity == owner)
            return;

        if (alreadyHit.Contains(otherNetEntity))
            return;

        if (otherNetEntity is PlayerEntity && owner is PlayerEntity)
            return;

        if (otherNetEntity is EnemyEntity && owner is EnemyEntity)
            return;

        alreadyHit.Add(otherNetEntity);

        otherNetEntity.ApplyDamageServer(damage);
    }

    private void DespawnSelf()
    {
        if (hasDespawned)
            return;

        hasDespawned = true;

        if (isServer)
            NetworkServer.Destroy(gameObject);
    }
}   