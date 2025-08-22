using UnityEngine;

public static class TargetHelper
{
    public static Transform FindClosestTarget(Vector3 origin, string tag, float range)
    {
        GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
        Transform closest = null;
        float minDist = range;

        foreach (var go in candidates)
        {
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
