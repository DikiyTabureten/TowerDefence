using System;
using System.Collections.Generic;

namespace TowerDefense
{
    /// <summary>Pure data: which towers the shop sells. No UI, no tower creation.</summary>
    public sealed class TowerShopModel
    {
        private readonly List<TowerConfig> _towers;

        public IReadOnlyList<TowerConfig> Towers => _towers;

        public TowerShopModel(IEnumerable<TowerConfig> towers)
        {
            if (towers == null) throw new ArgumentNullException(nameof(towers));
            _towers = new List<TowerConfig>(towers);
        }
    }
}
