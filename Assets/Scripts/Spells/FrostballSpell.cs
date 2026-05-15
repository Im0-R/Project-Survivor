using Mirror;
using UnityEngine;

public class FrostballSpell : Spell
{
    public FrostballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        int projectileCount = Mathf.Max(1, data.projectileCount);
        float spread = data.projectileSpreadAngle;

        Vector3 baseDirection = (target.position - owner.transform.position).normalized;
        baseDirection.y = 0f;

        float totalAngle = spread * (projectileCount - 1);
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + spread * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDirection;

            GameObject obj = GameObject.Instantiate(data.prefab, owner.transform.position, Quaternion.identity);
            Projectile proj = obj.GetComponent<Projectile>();

            proj?.Initialize(
                owner,
                target,
                data.damage,
                data.speed,
                data.currentLevel,
                data.pierceCount,
                dir
            );

            NetworkServer.Spawn(obj);
        }
    }
}