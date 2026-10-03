using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Marks an area where towers may be built. Put it on an object with a Collider2D
    /// on the BuildZone layer; TowerPlacementService checks positions against that layer.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BuildZone : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Collider2D zone = GetComponent<Collider2D>();
            if (zone == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawCube(zone.bounds.center, zone.bounds.size);
        }
    }
}
