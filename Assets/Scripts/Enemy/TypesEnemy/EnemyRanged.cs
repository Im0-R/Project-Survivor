using UnityEngine;

public class EnemyRanged : Enemy
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (!isServer) return;
        ChangeState(new EnemyChaseState());
    }
}
