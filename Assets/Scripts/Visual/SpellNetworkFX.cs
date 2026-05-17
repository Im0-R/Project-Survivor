using Mirror;
using UnityEngine;

public class SpellNetworkFX : NetworkBehaviour
{
    [ClientRpc]
    public void RpcSpawnProjectileVisual(
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
        if (ClientSpellVisualManager.Instance == null) return;

        ClientSpellVisualManager.Instance.SpawnProjectile(
            projectileId,
            visualId,
            motionType,
            start,
            target,
            direction,
            speed,
            lifetime,
            arcHeight);
    }

    [ClientRpc]
    public void RpcStopProjectileVisual(int projectileId)
    {
        if (ClientSpellVisualManager.Instance == null) return;

        ClientSpellVisualManager.Instance.StopProjectile(projectileId);
    }

    [ClientRpc]
    public void RpcSpawnImpactVisual(SpellVisualId visualId, Vector3 position)
    {
        if (ClientSpellVisualManager.Instance == null) return;

        ClientSpellVisualManager.Instance.SpawnImpact(visualId, position);
    }
}