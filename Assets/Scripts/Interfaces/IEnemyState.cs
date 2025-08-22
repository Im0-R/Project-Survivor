public interface  IEnemyState
{
    void Enter(Enemy enemy);      // Quand on entre dans l’état
    void Update(Enemy enemy);     // Ce que fait l’état chaque frame
    void Exit(Enemy enemy);       // Quand on sort de l’état
}
