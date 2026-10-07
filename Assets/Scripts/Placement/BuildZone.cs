using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// A single build spot. Exactly one tower can stand on it.
    /// Needs a Collider2D on the BuildZone layer; the tower is placed at the zone's center
    /// (or at the optional Placement Point).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BuildZone : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional. Where the tower stands. If empty, the center of the collider is used.")]
        private Transform _placementPoint;

        private Collider2D _collider;

        public Tower Occupant { get; private set; }

        // Unity's overloaded != also treats a destroyed tower as "no tower".
        public bool IsOccupied => Occupant != null;

        public Vector3 PlacementPosition
        {
            get
            {
                if (_placementPoint != null) return _placementPoint.position;

                if (_collider == null) _collider = GetComponent<Collider2D>();
                Vector3 center = _collider.bounds.center;
                center.z = 0f;
                return center;
            }
        }

        public void Occupy(Tower tower) => Occupant = tower;

        public void Release() => Occupant = null;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void OnDrawGizmos()
        {
            Collider2D zone = GetComponent<Collider2D>();
            if (zone == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);
        }
    }
}
