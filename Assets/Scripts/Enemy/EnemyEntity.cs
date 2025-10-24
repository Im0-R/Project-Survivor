using System.Collections.Generic;
using UnityEngine;

public class EnemyEntity : NetworkEntity
{
    public Transform firePoint;

    protected override void Awake()
    {
        base.Awake();
    }
}
