using System.Collections.Generic;
using UnityEngine;

public class ClientSpellVisualManager : MonoBehaviour
{
    public static ClientSpellVisualManager Instance { get; private set; }

    [SerializeField] private SpellVisualDatabase database;

    private readonly Dictionary<int, GameObject> activeProjectiles = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnProjectile(
        int projectileId,
        SpellVisualId visualId,
        ProjectileMotionType motionType,
        Vector3 start,
        Vector3 target,
        Vector3 direction,
        float speed,
        float lifetime,
        float arcHeight)
    {
        SpellVisualDatabase.Entry entry = database.Get(visualId);
        if (entry == null || entry.projectilePrefab == null) return;

        GameObject obj = Instantiate(entry.projectilePrefab, start, Quaternion.identity);
        activeProjectiles[projectileId] = obj;

        ProjectileVisual visual = obj.GetComponent<ProjectileVisual>();
        if (visual != null)
        {
            visual.Init(
                projectileId,
                motionType,
                start,
                target,
                direction,
                speed,
                lifetime,
                arcHeight);
        }
    }

    public void StopProjectile(int projectileId)
    {
        if (!activeProjectiles.TryGetValue(projectileId, out GameObject obj))
            return;

        activeProjectiles.Remove(projectileId);

        if (obj != null)
            Destroy(obj);
    }

    public void UnregisterProjectile(int projectileId)
    {
        activeProjectiles.Remove(projectileId);
    }

    public void SpawnImpact(SpellVisualId visualId, Vector3 position)
    {
        SpellVisualDatabase.Entry entry = database.Get(visualId);
        if (entry == null || entry.impactPrefab == null) return;

        GameObject obj = Instantiate(entry.impactPrefab, position, Quaternion.identity);
        Destroy(obj, 2f);
    }
}