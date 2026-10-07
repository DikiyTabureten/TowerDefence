using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    public sealed class ShooterTower : Tower, IAttackSpeedModifiable
    {
        [SerializeField, Tooltip("Where projectiles spawn. Defaults to the tower's position.")]
        private Transform _firePoint;

        private readonly ModifierStack _attackSpeed = new ModifierStack();
        private readonly List<Collider2D> _hits = new List<Collider2D>();

        private ShooterTowerConfig _config;
        private ProjectilePool _projectilePool;
        private ContactFilter2D _enemyFilter;
        private Enemy _target;
        private float _cooldown;

        /// <summary>Shots per second after all attack speed buffs.</summary>
        public float CurrentFireRate => _config.FireRate * _attackSpeed.Value;
        public float AttackSpeedMultiplier => _attackSpeed.Value;

        public override float Radius => _config != null ? _config.AttackRadius : 0f;

        protected override void OnInit(TowerConfig config, TowerContext context)
        {
            _config = RequireConfig<ShooterTowerConfig>(config);
            _projectilePool = context.ProjectilePool;

            _enemyFilter = new ContactFilter2D { useTriggers = true };
            _enemyFilter.SetLayerMask(context.EnemyMask);

            _cooldown = 0f;
        }

        private void Update()
        {
            if (_config == null) return;

            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);

            if (!IsValidTarget(_target))
                _target = FindTarget();

            if (_target == null || _cooldown > 0f) return;

            Fire(_target);
            _cooldown = 1f / CurrentFireRate;
        }

        private Enemy FindTarget()
        {
            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, _config.AttackRadius, _enemyFilter, _hits);

            Enemy best = null;
            float bestProgress = float.MinValue;

            // Prefer the enemy closest to the finish.
            foreach (Collider2D hit in _hits)
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (!IsValidTarget(enemy) || enemy.DistanceTravelled <= bestProgress) continue;

                best = enemy;
                bestProgress = enemy.DistanceTravelled;
            }

            return best;
        }

        private bool IsValidTarget(Enemy enemy)
        {
            return enemy != null
                && enemy.IsAlive
                && IsWithinRadius(transform.position, enemy.transform.position, _config.AttackRadius);
        }

        private void Fire(Enemy target)
        {
            Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;

            Projectile projectile = _projectilePool.Rent();
            projectile.transform.SetPositionAndRotation(origin, Quaternion.identity);
            projectile.Launch(target, _config.Damage, _config.ProjectileSpeed);
        }

        // IAttackSpeedModifiable
        public void AddAttackSpeedModifier(object source, float multiplier) => _attackSpeed.Set(source, multiplier);
        public void RemoveAttackSpeedModifier(object source) => _attackSpeed.Remove(source);
    }
}
