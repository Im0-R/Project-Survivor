using Mirror;
using UnityEngine;

public class FireballSpell : Spell
{
    private const float FixedSpreadAngle = 15f;

    public FireballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null) return;
        if (data == null || data.prefab == null) return;

        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is Enemy)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null)
            return;

        SpellData runtimeData = BuildRuntimeData(owner);
        runtimeData.damageType = DamageType.Fire;

        Vector3 baseDirection = target.position - owner.transform.position;
        baseDirection.y = 0f;

        if (baseDirection.sqrMagnitude <= 0.001f)
            baseDirection = owner.transform.forward;

        baseDirection.Normalize();

        Vector3 baseSpawnPosition = owner.transform.position + Vector3.up;
        float projectileScale = 1f;

        for (int i = 0; i < runtimeData.projectileCount; i++)
        {
            float angle = (i - (runtimeData.projectileCount - 1) * 0.5f) * FixedSpreadAngle;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;
            direction.y = 0f;
            direction.Normalize();

            Vector3 spawnPosition = baseSpawnPosition + direction * 0.6f;

            GameObject obj = GameObject.Instantiate(
                runtimeData.prefab,
                spawnPosition,
                Quaternion.LookRotation(direction)
            );

            Projectile projectile = obj.GetComponent<Projectile>();

            if (projectile == null)
            {
                GameObject.Destroy(obj);
                continue;
            }

            DamageInfo damageInfo = BuildDamageInfoFromRuntimeData(runtimeData);

            projectile.Initialize(
                owner,
                null,
                damageInfo,
                runtimeData.speed,
                projectileScale,
                runtimeData.pierceCount,
                direction
            );

            NetworkServer.Spawn(obj);
        }
    }
}