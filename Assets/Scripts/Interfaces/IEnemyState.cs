public interface  IEnemyState
{
    void Enter(Enemy enemy);      // Quand on entre dans l��tat
    void Update(Enemy enemy);     // Ce que fait l��tat chaque frame
    void Exit(Enemy enemy);       // Quand on sort de l��tat
}
