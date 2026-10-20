using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// The route enemies follow. Waypoint 0 is the spawn point, the last waypoint is the finish.
    /// Points come either from Transforms placed by hand (manual mode) or, at runtime,
    /// from a LevelConfig via SetPoints — then the Transforms are ignored.
    /// The road is wider than a line: each enemy walks its own lane, shifted sideways from the centre line.
    /// </summary>
    public sealed class WaypointPath : MonoBehaviour
    {
        [SerializeField, Tooltip("Manual mode only. Ignored once SetPoints is called (level loaded from LevelConfig).")]
        private Transform[] _waypoints;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far (units) from the centre line an enemy's lane may be.")]
        private float _laneSpread = 0.35f;

        private Vector3[] _points;

        public int Count => _points != null ? _points.Length : _waypoints != null ? _waypoints.Length : 0;

        /// <summary>Lanes go from -LaneSpread (right of the walking direction) to +LaneSpread (left).</summary>
        public float LaneSpread => _laneSpread;

        public Vector3 GetPoint(int index) => _points != null ? _points[index] : _waypoints[index].position;

        /// <summary>
        /// Waypoint of a lane parallel to the centre line, laneOffset units to the left of the walking direction.
        /// Corners use a miter join, so a lane keeps its distance from the centre line through every turn.
        /// </summary>
        public Vector3 GetPoint(int index, float laneOffset)
        {
            Vector3 point = GetPoint(index);
            if (Mathf.Approximately(laneOffset, 0f) || Count < 2) return point;

            bool hasIn = index > 0;
            bool hasOut = index < Count - 1;
            Vector2 normalIn = hasIn ? LeftNormal(GetPoint(index - 1), point) : Vector2.zero;
            Vector2 normalOut = hasOut ? LeftNormal(point, GetPoint(index + 1)) : Vector2.zero;

            if (!hasIn) return point + (Vector3)(normalOut * laneOffset);
            if (!hasOut) return point + (Vector3)(normalIn * laneOffset);

            Vector2 miter = (normalIn + normalOut).normalized;
            float cos = Mathf.Max(Vector2.Dot(miter, normalIn), 0.1f); // guard against near U-turns
            return point + (Vector3)(miter * (laneOffset / cos));
        }

        /// <summary>Use these world positions instead of the manual waypoints.</summary>
        public void SetPoints(IEnumerable<Vector3> points, float laneSpread)
        {
            _points = new List<Vector3>(points).ToArray();
            _laneSpread = Mathf.Max(0f, laneSpread);
        }

        private static Vector2 LeftNormal(Vector3 from, Vector3 to)
        {
            Vector2 direction = ((Vector2)(to - from)).normalized;
            return new Vector2(-direction.y, direction.x);
        }

        private void OnDrawGizmos()
        {
            if (Count < 2) return;
            if (_points == null && System.Array.Exists(_waypoints, w => w == null)) return;

            for (int i = 0; i < Count - 1; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));

                // Edges of the lane band
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
                Gizmos.DrawLine(GetPoint(i, _laneSpread), GetPoint(i + 1, _laneSpread));
                Gizmos.DrawLine(GetPoint(i, -_laneSpread), GetPoint(i + 1, -_laneSpread));
            }
        }
    }
}
