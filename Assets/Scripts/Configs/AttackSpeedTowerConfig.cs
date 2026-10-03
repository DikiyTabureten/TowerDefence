using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "AttackSpeedTowerConfig", menuName = "Tower Defense/Towers/Attack Speed Tower Config")]
    public sealed class AttackSpeedTowerConfig : AuraTowerConfig
    {
        [SerializeField, Min(1f), Tooltip("1.5 = affected towers fire 50% faster.")]
        private float _attackSpeedMultiplier = 1.5f;

        public float AttackSpeedMultiplier => _attackSpeedMultiplier;
    }
}
