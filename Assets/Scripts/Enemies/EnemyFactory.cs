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

        public Enemy Create(EnemyConfig config, WaypointPath path)
        {
            Enemy enemy = UnityEngine.Object.Instantiate(_prefab, path.GetPoint(0), Quaternion.identity, _parent);
            enemy.Init(config, path);
            return enemy;
        }
    }
}
