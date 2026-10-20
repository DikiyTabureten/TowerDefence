using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// The route enemies follow. Waypoint 0 is the spawn point, the last waypoint is the finish.
    /// Points come either from Transforms placed by hand (manual mode) or, at runtime,
    /// from a LevelConfig via SetPoints — then the Transforms are ignored.
    /// </summary>
    public sealed class WaypointPath : MonoBehaviour
    {
        [SerializeField, Tooltip("Manual mode only. Ignored once SetPoints is called (level loaded from LevelConfig).")]
        private Transform[] _waypoints;

        private Vector3[] _points;

        public int Count => _points != null ? _points.Length : _waypoints != null ? _waypoints.Length : 0;

        public Vector3 GetPoint(int index) => _points != null ? _points[index] : _waypoints[index].position;

        /// <summary>Use these world positions instead of the manual waypoints.</summary>
        public void SetPoints(IEnumerable<Vector3> points)
        {
            _points = new List<Vector3>(points).ToArray();
        }

        private void OnDrawGizmos()
        {
            if (Count < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < Count - 1; i++)
            {
                if (_points == null && (_waypoints[i] == null || _waypoints[i + 1] == null)) continue;
                Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));
            }
        }
    }
}
