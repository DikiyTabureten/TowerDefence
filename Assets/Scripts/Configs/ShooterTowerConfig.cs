using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "ShooterTowerConfig", menuName = "Tower Defense/Towers/Shooter Tower Config")]
    public sealed class ShooterTowerConfig : TowerConfig
    {
        [SerializeField, Min(0.1f)] private float _attackRadius = 3f;
        [SerializeField, Min(0f)] private float _damage = 10f;
        [SerializeField, Min(0.01f), Tooltip("Shots per second.")] private float _fireRate = 1f;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 8f;

        public float AttackRadius => _attackRadius;
        public float Damage => _damage;
        public float FireRate => _fireRate;
        public float ProjectileSpeed => _projectileSpeed;
    }
}
