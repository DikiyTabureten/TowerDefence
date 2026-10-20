using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Draws a level's terrain into three Tilemaps: Ground (grass, road, water), BuildSlots and Decoration.
    /// Layout comes from LevelConfig, tiles from MapTheme. Nothing is ever drawn over road cells
    /// or build slots. Purely visual: gameplay (path, zones) does not read the tilemaps.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public sealed class MapBuilder : MonoBehaviour
    {
        [SerializeField] private LevelConfig _level;
        [SerializeField] private MapTheme _theme;

        [Header("Tilemaps (children of this Grid)")]
        [SerializeField] private Tilemap _ground;
        [SerializeField] private Tilemap _buildSlots;
        [SerializeField] private Tilemap _decoration;

        [SerializeField, Min(0), Tooltip("Extra grass around the grid, so no empty background shows at the screen edges.")]
        private int _margin = 3;

        [SerializeField, Range(0f, 1f), Tooltip("Share of cells inside a Trees area that get a tree.")]
        private float _treeDensity = 0.7f;

        public LevelConfig Level => _level;

        [ContextMenu("Build Map")]
        public void Build() => Build(_level);

        public void Build(LevelConfig level)
        {
            if (level == null || _theme == null || _ground == null || _buildSlots == null || _decoration == null)
            {
                Debug.LogError("MapBuilder: assign Level, Theme and all three Tilemaps.", this);
                return;
            }

            _level = level;
            transform.position = level.Origin;               // cell (0,0) of the tilemaps = cell (0,0) of the level
            GetComponent<Grid>().cellSize = new Vector3(level.TileSize, level.TileSize, 0f);

            _ground.ClearAllTiles();
            _buildSlots.ClearAllTiles();
            _decoration.ClearAllTiles();

            var road = new HashSet<Vector2Int>();
            foreach (LevelConfig.PathData path in level.Paths)
                road.UnionWith(level.GetRoadCells(path, extendEnds: _margin));
            var slots = new HashSet<Vector2Int>(level.BuildSlots);
            var water = CellsOf(level, TerrainType.Water);

            DrawGround(level, road, water);
            foreach (Vector2Int slot in slots)
                if (!road.Contains(slot)) _buildSlots.SetTile(ToCell(slot), _theme.buildSlot);
            DrawDecoration(level, road, slots, water);

            // Grass edges depend on neighbouring road cells, so refresh after everything is placed.
            _ground.RefreshAllTiles();
        }

        private void DrawGround(LevelConfig level, HashSet<Vector2Int> road, HashSet<Vector2Int> water)
        {
            Vector2Int size = level.GridSize;
            for (int x = -_margin; x < size.x + _margin; x++)
            {
                for (int y = -_margin; y < size.y + _margin; y++)
                {
                    var cell = new Vector2Int(x, y);
                    TileBase tile = road.Contains(cell) ? _theme.road
                        : water.Contains(cell) ? _theme.water
                        : _theme.grass;
                    _ground.SetTile(ToCell(cell), tile);
                }
            }
        }

        private void DrawDecoration(LevelConfig level, HashSet<Vector2Int> road, HashSet<Vector2Int> slots, HashSet<Vector2Int> water)
        {
            foreach (LevelConfig.TerrainArea terrain in level.Terrain)
            {
                if (terrain.type == TerrainType.Water) continue; // part of the ground layer

                foreach (Vector2Int cell in terrain.area.allPositionsWithin)
                {
                    if (road.Contains(cell) || slots.Contains(cell) || water.Contains(cell)) continue;

                    TileBase tile = PickDecoration(terrain.type, cell);
                    if (tile != null) _decoration.SetTile(ToCell(cell), tile);
                }
            }
        }

        private TileBase PickDecoration(TerrainType type, Vector2Int cell)
        {
            switch (type)
            {
                case TerrainType.Trees:
                    return Hash(cell, 1) < _treeDensity ? Pick(_theme.trees, cell) : null;
                case TerrainType.Rocks:
                    return Pick(_theme.rocks, cell);
                case TerrainType.Houses:
                    return _theme.house;
                case TerrainType.CastleWall:
                    return _theme.castleWall;
                default:
                    return null;
            }
        }

        private static TileBase Pick(TileBase[] variants, Vector2Int cell)
        {
            if (variants == null || variants.Length == 0) return null;
            int index = Mathf.Min((int)(Hash(cell, 2) * variants.Length), variants.Length - 1);
            return variants[index];
        }

        /// <summary>Stable pseudo-random value in [0, 1) per cell: the same map every time it is built.</summary>
        private static float Hash(Vector2Int cell, int salt)
        {
            unchecked
            {
                uint h = (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static HashSet<Vector2Int> CellsOf(LevelConfig level, TerrainType type)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (LevelConfig.TerrainArea terrain in level.Terrain)
                if (terrain.type == type)
                    foreach (Vector2Int cell in terrain.area.allPositionsWithin)
                        cells.Add(cell);
            return cells;
        }

        private static Vector3Int ToCell(Vector2Int cell) => new Vector3Int(cell.x, cell.y, 0);

        private void OnDrawGizmos()
        {
            if (_level == null) return;

            // Grid outline
            Vector3 size = new Vector3(_level.GridSize.x, _level.GridSize.y, 0f) * _level.TileSize;
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawWireCube(_level.Origin + size * 0.5f, size);

            // Paths as enemies will walk them
            Gizmos.color = Color.yellow;
            foreach (LevelConfig.PathData path in _level.Paths)
                for (int i = 0; i < path.waypoints.Count - 1; i++)
                    Gizmos.DrawLine(_level.GridToWorld(path.waypoints[i]), _level.GridToWorld(path.waypoints[i + 1]));

            // Build slots
            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            foreach (Vector2Int slot in _level.BuildSlots)
                Gizmos.DrawWireCube(_level.GridToWorld(slot), Vector3.one * (_level.TileSize * 0.9f));
        }
    }
}
