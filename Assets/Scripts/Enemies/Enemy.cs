using System;
using UnityEngine;

namespace TowerDefense
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class Enemy : MonoBehaviour, ISlowable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private readonly ModifierStack _speedModifiers = new ModifierStack();

        private WaypointPath _path;
        private int _nextWaypointIndex;
        private bool _isAlive;

        public event Action<Enemy> Died;
        public event Action<Enemy> ReachedFinish;
        public event Action<float, float> HealthChanged; // current, max

        public EnemyConfig Config { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => _isAlive;

        /// <summary>How far along the path this enemy is. Towers use it to target the enemy closest to the finish.</summary>
        public float DistanceTravelled { get; private set; }

        public float CurrentSpeed => Config.MoveSpeed * _speedModifiers.Value;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(EnemyConfig config, WaypointPath path)
        {
            Config = config ? config : throw new ArgumentNullException(nameof(config));
            _path = path ? path : throw new ArgumentNullException(nameof(path));

            if (_path.Count < 2)
                throw new InvalidOperationException("WaypointPath needs at least two waypoints.");

            name = config.DisplayName;
            _spriteRenderer.sprite = config.Sprite;

            Health = config.MaxHealth;
            DistanceTravelled = 0f;
            transform.position = _path.GetPoint(0);
            _nextWaypointIndex = 1;
            _isAlive = true;
        }

        private void Update()
        {
            if (!_isAlive) return;

            Move(CurrentSpeed * Time.deltaTime);

            if (_nextWaypointIndex >= _path.Count)
                Finish();
        }

        private void Move(float distance)
        {
            // Loop so a fast enemy never "overshoots" a corner in one frame.
            while (distance > 0f && _nextWaypointIndex < _path.Count)
            {
                Vector3 position = transform.position;
                Vector3 target = _path.GetPoint(_nextWaypointIndex);
                float toTarget = Vector3.Distance(position, target);

                if (toTarget <= distance)
                {
                    transform.position = target;
                    DistanceTravelled += toTarget;
                    distance -= toTarget;
                    _nextWaypointIndex++;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(position, target, distance);
                    DistanceTravelled += distance;
                    distance = 0f;
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (!_isAlive || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            HealthChanged?.Invoke(Health, Config.MaxHealth);

            if (Health <= 0f)
                Die();
        }

        private void Die()
        {
            _isAlive = false;
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void Finish()
        {
            _isAlive = false;
            ReachedFinish?.Invoke(this);
            Destroy(gameObject);
        }

        // ISlowable
        public void AddSpeedModifier(object source, float multiplier) => _speedModifiers.Set(source, multiplier);
        public void RemoveSpeedModifier(object source) => _speedModifiers.Remove(source);
    }
}
