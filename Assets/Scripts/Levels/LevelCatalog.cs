using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Levels
{
    /// <summary>The ordered list of levels: the main menu shows them, "Next level" walks through them.</summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Tower Defense/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> _levels = new List<LevelConfig>();

        public IReadOnlyList<LevelConfig> Levels => _levels;
        public int Count => _levels.Count;

        public LevelConfig Get(int index) => index >= 0 && index < _levels.Count ? _levels[index] : null;

        public int IndexOf(LevelConfig level) => _levels.IndexOf(level);

        public bool HasNext(int index) => index >= 0 && index + 1 < _levels.Count;
    }
}
