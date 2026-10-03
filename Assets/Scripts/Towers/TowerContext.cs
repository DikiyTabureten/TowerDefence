using UnityEngine;

namespace TowerDefense
{
    /// <summary>Scene services a tower may need. Passed in by TowerFactory, so towers never search the scene.</summary>
    public sealed class TowerContext
    {
        public ProjectilePool ProjectilePool { get; }
        public LayerMask EnemyMask { get; }
        public LayerMask TowerMask { get; }

        public TowerContext(ProjectilePool projectilePool, LayerMask enemyMask, LayerMask towerMask)
        {
            ProjectilePool = projectilePool;
            EnemyMask = enemyMask;
            TowerMask = towerMask;
        }
    }
}
