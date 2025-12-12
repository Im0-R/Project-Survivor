using Mirror;
using UnityEngine;

public class FireballSpell : Spell
{
    //viable Constructor for Activator
    public FireballSpell() { }


    public override void ExecuteServer(NetworkEntity owner)
    {
        Transform target = null;

        NetworkEntity netOwner = owner; // (PlayerEntity or EnemyEntity)

        if (netOwner is PlayerEntity)
        {
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        }
        else if (netOwner is EnemyEntity)
        {
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);
        }

        if (target == null) return;

        //Instantiate the projectile
        GameObject obj = GameObject.Instantiate(data.prefab, owner.transform.position, Quaternion.identity);
        Projectile proj = obj.GetComponent<Projectile>();

        proj?.Initialize(netOwner, target, data.damage, data.speed, data.currentLevel);

        //Network spawn
        NetworkServer.Spawn(obj);
        Debug.Log($"{netOwner.entityName} cast FireballSpell ");
    }
}
