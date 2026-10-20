using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Turns a LevelConfig into the scene: draws the map, gives the enemy path its points
    /// and creates a BuildZone on every build slot. Created and called by Bootstrap;
    /// after Load the rest of the game works as before (spawner walks the path, placement finds the zones).
    /// </summary>
    public sealed class LevelLoader
    {
        private readonly MapBuilder _mapBuilder;
        private readonly WaypointPath _path;
        private readonly BuildZone _buildZonePrefab;
        private readonly Transform _buildZonesRoot;

        private readonly List<BuildZone> _zones = new List<BuildZone>();

        public LevelLoader(MapBuilder mapBuilder, WaypointPath path, BuildZone buildZonePrefab, Transform buildZonesRoot)
        {
            _mapBuilder = mapBuilder ? mapBuilder : throw new ArgumentNullException(nameof(mapBuilder));
            _path = path ? path : throw new ArgumentNullException(nameof(path));
            _buildZonePrefab = buildZonePrefab ? buildZonePrefab : throw new ArgumentNullException(nameof(buildZonePrefab));
            _buildZonesRoot = buildZonesRoot;
        }

        public void Load(LevelConfig level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (level.Paths.Count == 0) throw new InvalidOperationException($"Level '{level.name}' has no path.");

            _mapBuilder.Build(level);

            // One path for now; levels with several roads (Castle Siege) will pick a path per spawn group.
            _path.SetPoints(level.Paths[0].waypoints.Select(level.GridToWorld));

            CreateBuildZones(level);
        }

        private void CreateBuildZones(LevelConfig level)
        {
            foreach (BuildZone zone in _zones)
                if (zone != null) UnityEngine.Object.Destroy(zone.gameObject);
            _zones.Clear();

            foreach (Vector2Int slot in level.BuildSlots)
            {
                BuildZone zone = UnityEngine.Object.Instantiate(
                    _buildZonePrefab, level.GridToWorld(slot), Quaternion.identity, _buildZonesRoot);
                zone.name = $"BuildZone_{slot.x}_{slot.y}";
                _zones.Add(zone);
            }
        }
    }
}
