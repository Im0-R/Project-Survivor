public class Zombie : EnemyMelee
{
    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!isServer) return;
        ChangeState(new EnemyIdleState());
    }
}
