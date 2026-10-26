using UnityEngine;

namespace TowerDefense
{
    /// <summary>Scene services a tower may need. Passed in by TowerFactory, so towers never search the scene.</summary>
    public sealed class TowerContext
    {
        public ProjectilePool ProjectilePool { get; }
        public LayerMask EnemyMask { get; }
        public LayerMask TowerMask { get; }

        /// <summary>Beat of the level's music: shooter towers fire on its notes.</summary>
        public BeatConductor Conductor { get; }

        public TowerContext(ProjectilePool projectilePool, LayerMask enemyMask, LayerMask towerMask, BeatConductor conductor)
        {
            ProjectilePool = projectilePool;
            EnemyMask = enemyMask;
            TowerMask = towerMask;
            Conductor = conductor;
        }
    }
}
