using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>The only place towers are instantiated.</summary>
    public sealed class TowerFactory
    {
        private readonly TowerContext _context;
        private readonly Transform _parent;

        public TowerFactory(TowerContext context, Transform parent)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _parent = parent;
        }

        public Tower Create(TowerConfig config, Vector3 position)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Prefab == null)
                throw new InvalidOperationException($"TowerConfig '{config.name}' has no prefab assigned.");

            Tower tower = UnityEngine.Object.Instantiate(config.Prefab, position, Quaternion.identity, _parent);
            tower.name = config.DisplayName;
            tower.Init(config, _context);
            return tower;
        }
    }
}
