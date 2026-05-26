using Mirror;
using UnityEngine;

public class FireballSpell : Spell
{
    public FireballSpell() { }

    public override void ExecuteServer(NetworkEntity owner)
    {
        if (owner == null) return;
        if (data == null || data.prefab == null) return;

        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        PlayerEntity player = owner as PlayerEntity;
        string spellName = data.spellName;

        float finalDamage = data.damage;
        float finalSpeed = data.speed;
        int finalProjectileCount = data.projectileCount;
        int finalPierce = data.pierceCount;

        if (player != null)
        {
            finalDamage += player.GetCurrentStat(StatId.SpellDamage);
            finalDamage += player.GetCurrentStat(StatId.FireDamage);
            finalDamage += player.GetSpellModifier(spellName, "Damage");

            float damageMult = player.GetCurrentStat(StatId.DamageMult);
            finalDamage *= 1f + damageMult / 100f;

            finalSpeed += player.GetCurrentStat(StatId.ProjectileSpeed);
            finalSpeed += player.GetSpellModifier(spellName, "ProjectileSpeed");

            finalProjectileCount += Mathf.RoundToInt(player.GetSpellModifier(spellName, "ProjectileCount"));
            finalPierce += Mathf.RoundToInt(player.GetSpellModifier(spellName, "Pierce"));
        }

        finalProjectileCount = Mathf.Max(1, finalProjectileCount);
        finalPierce = Mathf.Max(0, finalPierce);

        float spread = data.projectileSpreadAngle;

        Vector3 baseDirection = target.position - owner.transform.position;
        baseDirection.y = 0f;

        if (baseDirection.sqrMagnitude <= 0.001f)
            baseDirection = owner.transform.forward;

        baseDirection.Normalize();

        float totalAngle = spread * (finalProjectileCount - 1);
        float startAngle = -totalAngle * 0.5f;

        Vector3 baseSpawnPosition = owner.transform.position + Vector3.up * 1f;

        for (int i = 0; i < finalProjectileCount; i++)
        {
            float angle = startAngle + spread * i;
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
                Debug.LogError("[Fireball] Projectile component missing on prefab.");
                GameObject.Destroy(obj);
                continue;
            }

            projectile.Initialize(
                owner,
                null,
                finalDamage,
                finalSpeed,
                data.currentLevel,
                finalPierce,
                direction
            );

            NetworkServer.Spawn(obj);
        }

        Debug.Log(
            $"[Fireball] count={finalProjectileCount}, damage={finalDamage}, speed={finalSpeed}, pierce={finalPierce}, cooldown={GetFinalCooldown(owner)}"
        );
    }
}