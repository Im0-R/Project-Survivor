using Unity.Netcode;
using UnityEngine;

public class FireballSpell : Spell
{

    public FireballSpell(GameObject prefab, Transform firePoint, float range, float cooldown ,bool autoCast = false)
        : base(cooldown, autoCast , firePoint , range , prefab)
    {
        data.prefab = prefab;
        data.firePoint = firePoint;
        data.range = range;

    }

    public override void ExecuteServer(Entity owner)
    {
        Transform target = null;

        var netOwner = owner.NetEntity; // le NetworkEntity réel (PlayerEntity ou EnemyEntity)

        if (netOwner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(data.firePoint.position, "Enemy", data.range);
        else if (netOwner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(data.firePoint.position, "Player", data.range);

        if (target == null) return;

        var obj = GameObject.Instantiate(data.prefab, data.firePoint.position, Quaternion.identity);
        var proj = obj.GetComponent<Projectile>();

        // ✅ nouvelle signature avec cible
        proj?.Initialize(netOwner, target, 10f, 10f);

        obj.GetComponent<NetworkObject>()?.Spawn(true);
    }
}
