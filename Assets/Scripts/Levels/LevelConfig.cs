using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Levels
{
    public enum TerrainType
    {
        Trees,
        Water,
        Rocks,
        Houses,
        CastleWall
    }

    /// <summary>
    /// Everything that describes one level: the grid, the enemy path(s), build slots, economy, waves
    /// and the terrain decoration. One asset = one level. Cells are grid coordinates;
    /// the grid is centred on the world origin and one cell is one world unit (see GridToWorld).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Tower Defense/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Serializable]
        public sealed class PathData
        {
            [Tooltip("Turning points of the road in grid cells. Neighbouring points must share X or Y. " +
                     "The first and last points may lie outside the grid (enemies walk in from off-screen).")]
            public List<Vector2Int> waypoints = new List<Vector2Int>();
        }

        [Serializable]
        public sealed class SpawnGroup
        {
            public EnemyConfig enemy;
            [Min(1)] public int count = 5;
            [Min(0f), Tooltip("Seconds between two enemies of this group.")]
            public float spawnInterval = 1f;
            [Min(0f), Tooltip("Seconds after the wave starts before this group begins. Groups of a wave run in parallel.")]
            public float delayBeforeGroup;
            [Min(0), Tooltip("Which road (index in Paths) the group walks.")]
            public int pathIndex;
        }

        [Serializable]
        public sealed class WaveData
        {
            public List<SpawnGroup> groups = new List<SpawnGroup>();
        }

        [Serializable]
        public sealed class TerrainArea
        {
            public TerrainType type;
            [Tooltip("Cells covered: x, y = bottom-left cell; width, height in cells.")]
            public RectInt area;
        }

        [Header("Info")]
        [SerializeField] private string _displayName = "Level";
        [SerializeField, TextArea] private string _description;

        [Header("Grid")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(12, 7);
        [SerializeField, Min(0.1f)] private float _tileSize = 1f;

        [Header("Layout")]
        [SerializeField] private List<PathData> _paths = new List<PathData>();
        [SerializeField] private List<Vector2Int> _buildSlots = new List<Vector2Int>();
        [SerializeField] private List<TerrainArea> _terrain = new List<TerrainArea>();

        [Header("Economy")]
        [SerializeField, Min(0)] private int _startingGold = 150;
        [SerializeField, Min(1)] private int _startingLives = 10;

        [Header("Waves")]
        [SerializeField, Min(0f), Tooltip("Pause before each wave, counted from the moment the previous wave finished spawning.")]
        private float _timeBetweenWaves = 5f;
        [SerializeField] private List<WaveData> _waves = new List<WaveData>();

        public string DisplayName => _displayName;
        public string Description => _description;
        public Vector2Int GridSize => _gridSize;
        public float TileSize => _tileSize;
        public IReadOnlyList<PathData> Paths => _paths;
        public IReadOnlyList<Vector2Int> BuildSlots => _buildSlots;
        public IReadOnlyList<TerrainArea> Terrain => _terrain;
        public int StartingGold => _startingGold;
        public int StartingLives => _startingLives;
        public float TimeBetweenWaves => _timeBetweenWaves;
        public IReadOnlyList<WaveData> Waves => _waves;

        /// <summary>World position of the grid's bottom-left corner. The grid is centred on (0, 0).</summary>
        public Vector3 Origin => new Vector3(-_gridSize.x * _tileSize * 0.5f, -_gridSize.y * _tileSize * 0.5f, 0f);

        /// <summary>Centre of a cell in world space.</summary>
        public Vector3 GridToWorld(Vector2Int cell) =>
            Origin + new Vector3((cell.x + 0.5f) * _tileSize, (cell.y + 0.5f) * _tileSize, 0f);

        public bool IsInsideGrid(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < _gridSize.x && cell.y < _gridSize.y;

        /// <summary>
        /// Every cell the road of a path covers: straight segments between its waypoints.
        /// With extendEnds &gt; 0 the road continues that many cells beyond the first and last waypoint,
        /// so it visibly runs off the edge of the map.
        /// </summary>
        public List<Vector2Int> GetRoadCells(PathData path, int extendEnds = 0)
        {
            var cells = new List<Vector2Int>();
            List<Vector2Int> points = path.waypoints;
            if (points == null || points.Count == 0) return cells;
            if (points.Count == 1) { cells.Add(points[0]); return cells; }

            AddSegment(cells, points[0] - Direction(points[0], points[1]) * extendEnds, points[0]);
            for (int i = 0; i < points.Count - 1; i++)
                AddSegment(cells, points[i], points[i + 1]);
            Vector2Int last = points[points.Count - 1];
            AddSegment(cells, last, last + Direction(points[points.Count - 2], last) * extendEnds);
            return cells;
        }

        /// <summary>Layout mistakes that would break the level. Shown in the Console when the asset changes.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var road = new HashSet<Vector2Int>();

            if (_paths.Count == 0) problems.Add("no paths");
            for (int p = 0; p < _paths.Count; p++)
            {
                List<Vector2Int> points = _paths[p].waypoints;
                if (points.Count < 2) problems.Add($"path {p} needs at least two waypoints");
                for (int i = 0; i < points.Count - 1; i++)
                    if (points[i].x != points[i + 1].x && points[i].y != points[i + 1].y)
                        problems.Add($"path {p}: segment {points[i]} -> {points[i + 1]} is diagonal");
                if (points.Count >= 2) road.UnionWith(GetRoadCells(_paths[p]));
            }

            foreach (Vector2Int slot in _buildSlots)
            {
                if (!IsInsideGrid(slot)) problems.Add($"build slot {slot} is outside the grid");
                if (road.Contains(slot)) problems.Add($"build slot {slot} is on the road");
            }

            for (int w = 0; w < _waves.Count; w++)
            {
                foreach (SpawnGroup group in _waves[w].groups)
                {
                    if (group.enemy == null) problems.Add($"wave {w + 1}: a group has no enemy");
                    if (group.pathIndex >= _paths.Count) problems.Add($"wave {w + 1}: path index {group.pathIndex} does not exist");
                }
            }

            return problems;
        }

        private void OnValidate()
        {
            foreach (string problem in Validate())
                Debug.LogWarning($"[LevelConfig] {name}: {problem}", this);
        }

        private static Vector2Int Direction(Vector2Int from, Vector2Int to) =>
            new Vector2Int(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));

        private static void AddSegment(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
        {
            if (from.x != to.x && from.y != to.y)
            {
                Debug.LogWarning($"Path segment {from} -> {to} is diagonal; roads must be horizontal or vertical.");
                return;
            }

            Vector2Int step = Direction(from, to);
            Vector2Int cell = from;
            if (!cells.Contains(cell)) cells.Add(cell);
            while (cell != to && step != Vector2Int.zero)
            {
                cell += step;
                if (!cells.Contains(cell)) cells.Add(cell);
            }
        }
    }
}
