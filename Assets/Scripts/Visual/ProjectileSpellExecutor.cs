using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpellExecutor : ISpellExecutor
{
    private readonly SpellCasterServer caster;
    private static int nextProjectileId = 1;

    public ProjectileSpellExecutor(SpellCasterServer caster)
    {
        this.caster = caster;
    }

    public void Execute(Spell.SpellData data, NetworkEntity owner)
    {
        Transform target = null;

        if (owner is PlayerEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Enemy", data.range);
        else if (owner is EnemyEntity)
            target = TargetHelper.FindClosestTarget(owner.transform.position, "Player", data.range);

        if (target == null) return;

        Vector3 start = owner.transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;
        Vector3 direction = (targetPos - start).normalized;

        LayerMask hitMask = owner is PlayerEntity
            ? LayerMask.GetMask("Enemy")
            : LayerMask.GetMask("Player");

        SpellNetworkFX fx = owner.GetComponent<SpellNetworkFX>();
        if (fx == null) return;

        for (int i = 0; i < Mathf.Max(1, data.projectileCount); i++)
        {
            Vector3 spreadDirection = ApplySpread(
                direction,
                i,
                data.projectileCount,
                data.projectileSpreadAngle);

            int projectileId = GetNextProjectileId();

            fx.RpcSpawnProjectileVisual(
                projectileId,
                data.visualId,
                data.motionType,
                start,
                targetPos,
                spreadDirection,
                data.speed,
                data.projectileLifetime,
                data.arcHeight);

            caster.StartCoroutine(ProjectileRoutine(
                projectileId,
                data,
                owner,
                start,
                targetPos,
                spreadDirection,
                hitMask));
        }
    }

    private static int GetNextProjectileId()
    {
        nextProjectileId++;

        if (nextProjectileId <= 0)
            nextProjectileId = 1;

        return nextProjectileId;
    }

    private Vector3 ApplySpread(
        Vector3 direction,
        int index,
        int count,
        float spreadAngle)
    {
        if (count <= 1)
            return direction;

        float startAngle = -spreadAngle * (count - 1) * 0.5f;
        float angle = startAngle + spreadAngle * index;

        return Quaternion.AngleAxis(angle, Vector3.up) * direction;
    }

    private IEnumerator ProjectileRoutine(
        int projectileId,
        Spell.SpellData data,
        NetworkEntity owner,
        Vector3 start,
        Vector3 target,
        Vector3 direction,
        LayerMask hitMask)
    {
        SpellNetworkFX fx = owner.GetComponent<SpellNetworkFX>();

        float elapsed = 0f;
        int hitCount = 0;

        Vector3 previousPosition = start;
        Vector3 currentPosition = start;

        HashSet<NetworkEntity> alreadyHit = new HashSet<NetworkEntity>();

        while (elapsed < data.projectileLifetime)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            currentPosition = GetNextPosition(
                data,
                start,
                target,
                currentPosition,
                direction,
                elapsed,
                delta);

            Vector3 segment = currentPosition - previousPosition;
            float distance = segment.magnitude;

            if (distance > 0f)
            {
                if (Physics.SphereCast(
                    previousPosition,
                    data.projectileRadius,
                    segment.normalized,
                    out RaycastHit hit,
                    distance,
                    hitMask))
                {
                    NetworkEntity hitEntity = hit.collider.GetComponentInParent<NetworkEntity>();

                    if (IsValidTarget(owner, hitEntity) && !alreadyHit.Contains(hitEntity))
                    {
                        alreadyHit.Add(hitEntity);
                        hitCount++;

                        hitEntity.ApplyDamageServer(data.damage);

                        if (fx != null)
                            fx.RpcSpawnImpactVisual(data.visualId, hit.point);

                        if (hitCount >= data.maxHits)
                        {
                            if (fx != null)
                                fx.RpcStopProjectileVisual(projectileId);

                            yield break;
                        }

                        previousPosition = hit.point + direction * 0.2f;
                        currentPosition = previousPosition;
                    }
                }
            }

            previousPosition = currentPosition;
            yield return null;
        }

        if (fx != null)
            fx.RpcStopProjectileVisual(projectileId);
    }

    private Vector3 GetNextPosition(
        Spell.SpellData data,
        Vector3 start,
        Vector3 target,
        Vector3 current,
        Vector3 direction,
        float elapsed,
        float delta)
    {
        float t = data.projectileLifetime <= 0f
            ? 1f
            : Mathf.Clamp01(elapsed / data.projectileLifetime);

        switch (data.motionType)
        {
            case ProjectileMotionType.Straight:
                return current + direction * data.speed * delta;

            case ProjectileMotionType.TargetPosition:
                return Vector3.Lerp(start, target, t);

            case ProjectileMotionType.Arc:
                Vector3 pos = Vector3.Lerp(start, target, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * data.arcHeight;
                return pos;

            default:
                return current;
        }
    }

    private bool IsValidTarget(NetworkEntity owner, NetworkEntity target)
    {
        if (owner == null || target == null) return false;
        if (target == owner) return false;

        if (owner is PlayerEntity && target is PlayerEntity) return false;
        if (owner is EnemyEntity && target is EnemyEntity) return false;

        return true;
    }
}