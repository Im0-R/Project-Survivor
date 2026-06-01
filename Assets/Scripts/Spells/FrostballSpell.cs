using Mirror;
using UnityEngine;

public class FrostballSpell : Spell
{
    private const float FixedSpreadAngle = 15f;

    public FrostballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null) return;
        if (data == null || data.prefab == null) return;

        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null)
        {
            Debug.LogWarning("[Frostball] No target found.");
            return;
        }

        float finalDamage = GetFinalDamage(owner);
        float finalSpeed = GetFinalProjectileSpeed(owner);
        int finalProjectileCount = GetFinalProjectileCount(owner);
        int finalPierce = GetFinalPierce(owner);

        Vector3 baseDirection = target.position - owner.transform.position;
        baseDirection.y = 0f;

        if (baseDirection.sqrMagnitude <= 0.001f)
            baseDirection = owner.transform.forward;

        baseDirection.Normalize();

        Vector3 baseSpawnPosition = owner.transform.position + Vector3.up * 1f;
        float projectileScale = 1f;

        for (int i = 0; i < finalProjectileCount; i++)
        {
            float angle = (i - (finalProjectileCount - 1) * 0.5f) * FixedSpreadAngle;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            direction.y = 0f;
            direction.Normalize();

            Vector3 spawnPosition = baseSpawnPosition + direction * 0.6f;

            GameObject obj = GameObject.Instantiate(
                data.prefab,
                spawnPosition,
                Quaternion.LookRotation(direction)
            );

            Projectile projectile = obj.GetComponent<Projectile>();

            if (projectile == null)
            {
                Debug.LogError("[Frostball] Projectile component missing on prefab.");
                GameObject.Destroy(obj);
                continue;
            }

            projectile.Initialize(
                owner,
                null,
                finalDamage,
                finalSpeed,
                projectileScale,
                finalPierce,
                direction
            );

            NetworkServer.Spawn(obj);
        }

        Debug.Log($"[Frostball] count={finalProjectileCount}, damage={finalDamage}, speed={finalSpeed}, pierce={finalPierce}, cooldown={GetFinalCooldown(owner)}");
    }
}