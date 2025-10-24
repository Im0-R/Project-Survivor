using UnityEngine;

public class EnemyMelee : Enemy
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (!isServer) return;

        // Commence en Idle ou directement en Chase
        ChangeState(new EnemyChaseState());
    }
}
