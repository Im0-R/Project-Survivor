using Mirror;
using UnityEngine;

public class FrostballSpell : Spell
{
    public FrostballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (data == null || data.prefab == null) return;

        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        int projectileCount = Mathf.Max(1, data.projectileCount);
        float spread = data.projectileSpreadAngle;

        Vector3 baseDirection = target.position - owner.transform.position;
        baseDirection.y = 0f;

        if (baseDirection == Vector3.zero)
            baseDirection = owner.transform.forward;

        baseDirection.Normalize();

        float totalAngle = spread * (projectileCount - 1);
        float startAngle = -totalAngle * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + spread * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;

            GameObject obj = GameObject.Instantiate(
                data.prefab,
                owner.transform.position,
                Quaternion.LookRotation(direction)
            );

            Projectile projectile = obj.GetComponent<Projectile>();

            projectile?.Initialize(
                owner,
                target,
                data.damage,
                data.speed,
                data.currentLevel,
                data.pierceCount,
                direction
            );

            NetworkServer.Spawn(obj);
        }

        Debug.Log($"{owner.StatComp.Name} cast Frostball Arcana | projectiles={projectileCount} | pierce={data.pierceCount}");
    }
}