using System;
using UnityEngine;

namespace TowerDefense
{
    public sealed class EnemyFactory
    {
        private readonly Enemy _prefab;
        private readonly Transform _parent;

        public EnemyFactory(Enemy prefab, Transform parent)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
        }

        /// <summary>Creates an enemy on a random lane of the road, so a wave spreads over the road's width.</summary>
        public Enemy Create(EnemyConfig config, WaypointPath path)
        {
            float laneOffset = UnityEngine.Random.Range(-path.LaneSpread, path.LaneSpread);
            Enemy enemy = UnityEngine.Object.Instantiate(_prefab, path.GetPoint(0, laneOffset), Quaternion.identity, _parent);
            enemy.Init(config, path, laneOffset);
            return enemy;
        }
    }
}
