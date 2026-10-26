using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Fires on the notes of the level's music (BeatConductor.NoteHit) instead of a cooldown.
    /// Fire rate and attack speed buffs still matter: they decide how many notes pass between shots.
    /// </summary>
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
        private BeatConductor _conductor;

        /// <summary>Raised right after a projectile is launched. Used by visuals (muzzle flash, recoil).</summary>
        public event Action<Enemy> Fired;

        /// <summary>The enemy the tower is currently aiming at, or null. Used by visuals to turn the turret.</summary>
        public Enemy CurrentTarget => IsValidTarget(_target) ? _target : null;

        /// <summary>Shots per second after all attack speed buffs.</summary>
        public float CurrentFireRate => _config.FireRate * _attackSpeed.Value;
        public float AttackSpeedMultiplier => _attackSpeed.Value;

        /// <summary>
        /// Shoot on every N-th note. N = notes per second / shots per second, rounded to 1, 2, 4, 8…
        /// so the shots stay on the beat. Faster fire rate (e.g. the Orchestra's buff) = smaller N.
        /// </summary>
        public int NotesPerShot
        {
            get
            {
                if (_conductor == null || _conductor.Track == null || CurrentFireRate <= 0f) return 1;
                return Mathf.Max(1, Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(_conductor.Track.NotesPerSecond / CurrentFireRate)));
            }
        }

        public override float Radius => _config != null ? _config.AttackRadius : 0f;

        protected override void OnInit(TowerConfig config, TowerContext context)
        {
            _config = RequireConfig<ShooterTowerConfig>(config);
            _projectilePool = context.ProjectilePool;

            _enemyFilter = new ContactFilter2D { useTriggers = true };
            _enemyFilter.SetLayerMask(context.EnemyMask);

            _conductor = context.Conductor;
            if (_conductor != null)
                _conductor.NoteHit += OnNoteHit;
            else
                Debug.LogError("ShooterTower: no BeatConductor in TowerContext — the tower will not fire. Assign it in Bootstrap.", this);
        }

        private void OnDestroy()
        {
            if (_conductor != null) _conductor.NoteHit -= OnNoteHit;
        }

        private void Update()
        {
            // Only keeps the target up to date; shots happen on notes (OnNoteHit).
            if (_config == null) return;

            if (!IsValidTarget(_target))
                _target = FindTarget();
        }

        private void OnNoteHit(int noteIndex, RhythmNote note)
        {
            if (_config == null || noteIndex % NotesPerShot != 0) return;

            if (!IsValidTarget(_target))
                _target = FindTarget();

            if (_target != null) Fire(_target); // no target: this note is skipped
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
            return _config != null
                && enemy != null
                && enemy.IsAlive
                && IsWithinRadius(transform.position, enemy.transform.position, _config.AttackRadius);
        }

        private void Fire(Enemy target)
        {
            Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;

            Projectile projectile = _projectilePool.Rent();
            projectile.transform.SetPositionAndRotation(origin, Quaternion.identity);
            projectile.Launch(target, _config.Damage, _config.ProjectileSpeed);

            Fired?.Invoke(target);
        }

        // IAttackSpeedModifiable
        public void AddAttackSpeedModifier(object source, float multiplier) => _attackSpeed.Set(source, multiplier);
        public void RemoveAttackSpeedModifier(object source) => _attackSpeed.Remove(source);
    }
}
