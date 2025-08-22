using UnityEngine;

public class EnemyRanged : Enemy
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        ChangeState(new EnemyChaseState());
    }
}
