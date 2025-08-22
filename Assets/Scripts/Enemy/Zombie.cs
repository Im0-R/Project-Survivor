using UnityEngine;

public class Zombie : EnemyMelee
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        ChangeState(new EnemyIdleState());
    }
}
