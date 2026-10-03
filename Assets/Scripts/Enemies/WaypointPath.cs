using UnityEngine;

namespace TowerDefense
{
    /// <summary>The route enemies follow. Waypoint 0 is the spawn point, the last waypoint is the finish.</summary>
    public sealed class WaypointPath : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;

        public int Count => _waypoints != null ? _waypoints.Length : 0;

        public Vector3 GetPoint(int index) => _waypoints[index].position;

        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _waypoints.Length - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
            }
        }
    }
}
