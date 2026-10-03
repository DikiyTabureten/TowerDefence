using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "SlowTowerConfig", menuName = "Tower Defense/Towers/Slow Tower Config")]
    public sealed class SlowTowerConfig : AuraTowerConfig
    {
        [SerializeField, Range(0.05f, 1f), Tooltip("0.5 = affected enemies move twice as slowly.")]
        private float _speedMultiplier = 0.5f;

        public float SpeedMultiplier => _speedMultiplier;
    }
}
