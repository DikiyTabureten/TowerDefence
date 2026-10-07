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
            transform.rotation = Quaternion.identity;
            _spriteRenderer.flipX = false;
            _nextWaypointIndex = 1;
            _isAlive = true;

            UpdateFacing(instant: true);
        }

        private void Update()
        {
            if (!_isAlive) return;

            Move(CurrentSpeed * Time.deltaTime);

            if (_nextWaypointIndex >= _path.Count)
            {
                Finish();
                return;
            }

            UpdateFacing(instant: false);
        }

        private void UpdateFacing(bool instant)
        {
            Vector2 direction = _path.GetPoint(_nextWaypointIndex) - transform.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            switch (Config.FacingMode)
            {
                case EnemyFacingMode.RotateTowardsWaypoint:
                {
                    float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - Config.SpriteFacingAngle;
                    float angle = instant || Config.TurnSpeed <= 0f
                        ? targetAngle
                        : Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, Config.TurnSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    break;
                }
                case EnemyFacingMode.FlipHorizontally:
                {
                    if (Mathf.Abs(direction.x) < 0.01f) break; // moving straight up/down: keep current look
                    bool spriteLooksLeft = Mathf.Abs(Mathf.DeltaAngle(Config.SpriteFacingAngle, 180f)) < 90f;
                    _spriteRenderer.flipX = (direction.x < 0f) != spriteLooksLeft;
                    break;
                }
            }
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
