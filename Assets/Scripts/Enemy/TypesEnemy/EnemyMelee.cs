using UnityEngine;

public class EnemyMelee : Enemy
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        // Commence en Idle ou directement en Chase
        ChangeState(new EnemyChaseState());
    }
}
