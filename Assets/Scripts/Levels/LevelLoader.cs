using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefense.Levels
{
    /// <summary>What a loaded level gives the rest of the game.</summary>
    public sealed class LoadedLevel
    {
        public LevelConfig Config { get; }
        public IReadOnlyList<WaypointPath> Paths { get; }

        public LoadedLevel(LevelConfig config, IReadOnlyList<WaypointPath> paths)
        {
            Config = config;
            Paths = paths;
        }
    }

    /// <summary>
    /// Turns a LevelConfig into the scene: draws the map, creates one WaypointPath per road,
    /// a BuildZone on every build slot, and fits the camera to the grid. Created and called by Bootstrap;
    /// after Load the rest of the game works as before (spawner walks the paths, placement finds the zones).
    /// </summary>
    public sealed class LevelLoader
    {
        private readonly MapBuilder _mapBuilder;
        private readonly BuildZone _buildZonePrefab;
        private readonly Transform _buildZonesRoot;
        private readonly Transform _pathsRoot;
        private readonly Camera _camera;

        public LevelLoader(MapBuilder mapBuilder, BuildZone buildZonePrefab, Transform buildZonesRoot,
            Transform pathsRoot, Camera camera)
        {
            _mapBuilder = mapBuilder ? mapBuilder : throw new ArgumentNullException(nameof(mapBuilder));
            _buildZonePrefab = buildZonePrefab ? buildZonePrefab : throw new ArgumentNullException(nameof(buildZonePrefab));
            _buildZonesRoot = buildZonesRoot;
            _pathsRoot = pathsRoot;
            _camera = camera;
        }

        public LoadedLevel Load(LevelConfig level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (level.Paths.Count == 0) throw new InvalidOperationException($"Level '{level.name}' has no path.");

            foreach (string problem in level.Validate())
                Debug.LogWarning($"[LevelLoader] {level.name}: {problem}", level);

            _mapBuilder.Build(level);
            List<WaypointPath> paths = CreatePaths(level);
            CreateBuildZones(level);
            FitCamera(level);

            return new LoadedLevel(level, paths);
        }

        private List<WaypointPath> CreatePaths(LevelConfig level)
        {
            var paths = new List<WaypointPath>();
            for (int i = 0; i < level.Paths.Count; i++)
            {
                var go = new GameObject($"Path_{i}");
                go.transform.SetParent(_pathsRoot, false);
                var path = go.AddComponent<WaypointPath>();
                path.SetPoints(level.Paths[i].waypoints.Select(level.GridToWorld), level.LaneSpread);
                paths.Add(path);
            }
            return paths;
        }

        private void CreateBuildZones(LevelConfig level)
        {
            foreach (Vector2Int slot in level.BuildSlots)
            {
                BuildZone zone = UnityEngine.Object.Instantiate(
                    _buildZonePrefab, level.GridToWorld(slot), Quaternion.identity, _buildZonesRoot);
                zone.name = $"BuildZone_{slot.x}_{slot.y}";
            }
        }

        /// <summary>The whole grid is visible whatever the window's aspect ratio. The grid is centred on (0, 0).</summary>
        private void FitCamera(LevelConfig level)
        {
            if (_camera == null || !_camera.orthographic) return;

            float width = level.GridSize.x * level.TileSize;
            float height = level.GridSize.y * level.TileSize;
            _camera.orthographicSize = Mathf.Max(height * 0.5f, width * 0.5f / _camera.aspect);
            _camera.transform.position = new Vector3(0f, 0f, _camera.transform.position.z);
        }
    }
}
