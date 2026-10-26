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

        /// <summary>Plays the sound of a shot as a note of the melody, at an exact time. May be null (silent shots).</summary>
        public IShotNoteSink ShotSounds { get; }

        public TowerContext(ProjectilePool projectilePool, LayerMask enemyMask, LayerMask towerMask,
            BeatConductor conductor, IShotNoteSink shotSounds)
        {
            ProjectilePool = projectilePool;
            EnemyMask = enemyMask;
            TowerMask = towerMask;
            Conductor = conductor;
            ShotSounds = shotSounds;
        }
    }
}
