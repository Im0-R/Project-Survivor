using UnityEngine;

public static class TargetHelper
{
    public static Transform FindClosestEnemyTarget(NetworkEntity owner, float range)
    {
        if (owner == null)
            return null;

        string targetTag = GetEnemyTag(owner);

        if (string.IsNullOrEmpty(targetTag))
            return null;

        return FindClosestTarget(owner.transform.position, targetTag, range);
    }

    public static string GetEnemyTag(NetworkEntity owner)
    {
        if (owner is PlayerEntity)
            return "Enemy";

        if (owner is Enemy)
            return "Player";

        return null;
    }

    public static bool CanDamage(NetworkEntity owner, NetworkEntity target)
    {
        if (owner == null || target == null)
            return false;

        if (owner == target)
            return false;

        if (owner is PlayerEntity && target is Enemy)
            return true;

        if (owner is Enemy && target is PlayerEntity)
            return true;

        return false;
    }

    public static Transform FindClosestTarget(Vector3 origin, string tag, float range)
    {
        GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);

        Transform closest = null;
        float minDist = range;

        foreach (GameObject go in candidates)
        {
            if (go == null)
                continue;

            float dist = Vector3.Distance(origin, go.transform.position);

            if (dist <= minDist)
            {
                minDist = dist;
                closest = go.transform;
            }
        }

        return closest;
    }
}