using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class Enemy : MonoBehaviour, ISlowable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Lane (walking side by side, overtaking)")]
        [SerializeField, Min(0f), Tooltip("Sideways speed (units/s) when moving to another lane.")]
        private float _laneChangeSpeed = 1.2f;
        [SerializeField, Min(0f), Tooltip("How far ahead (units along the road) to look for slower enemies.")]
        private float _lookAhead = 0.9f;
        [SerializeField, Min(0f), Tooltip("Sideways distance kept from a slower enemy when overtaking it.")]
        private float _minLaneGap = 0.45f;
        [SerializeField, Min(0.02f), Tooltip("Seconds between checks for slower enemies ahead.")]
        private float _avoidanceInterval = 0.1f;

        private readonly ModifierStack _speedModifiers = new ModifierStack();
        private readonly List<Collider2D> _neighbours = new List<Collider2D>();

        private WaypointPath _path;
        private int _nextWaypointIndex;
        private bool _isAlive;
        private float _laneOffset;        // current sideways offset from the road's centre line
        private float _targetLaneOffset;  // where the enemy is moving sideways to
        private float _avoidanceTimer;
        private ContactFilter2D _crowdFilter;

        public event Action<Enemy> Died;
        public event Action<Enemy> ReachedFinish;
        public event Action<float, float> HealthChanged; // current, max

        /// <summary>Raised with the new combined speed multiplier (1 = normal, below 1 = slowed).</summary>
        public event Action<float> SpeedMultiplierChanged;

        public EnemyConfig Config { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => _isAlive;

        /// <summary>How far along the path this enemy is. Towers use it to target the enemy closest to the finish.</summary>
        public float DistanceTravelled { get; private set; }

        /// <summary>Product of all speed modifiers currently applied (1 = no slow).</summary>
        public float SpeedMultiplier => _speedModifiers.Value;

        public float CurrentSpeed => Config.MoveSpeed * _speedModifiers.Value;

        /// <summary>Sideways offset from the road's centre line (positive = left of the walking direction).</summary>
        public float LaneOffset => _laneOffset;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _speedModifiers.Changed += OnSpeedModifiersChanged;

            // Other enemies are on the same physics layer as this one.
            _crowdFilter = new ContactFilter2D { useTriggers = true };
            _crowdFilter.SetLayerMask(1 << gameObject.layer);
        }

        private void OnDestroy()
        {
            _speedModifiers.Changed -= OnSpeedModifiersChanged;
        }

        /// <param name="laneOffset">Sideways offset from the road's centre line, within ±path.LaneSpread.</param>
        public void Init(EnemyConfig config, WaypointPath path, float laneOffset = 0f)
        {
            Config = config ? config : throw new ArgumentNullException(nameof(config));
            _path = path ? path : throw new ArgumentNullException(nameof(path));

            if (_path.Count < 2)
                throw new InvalidOperationException("WaypointPath needs at least two waypoints.");

            name = config.DisplayName;
            _spriteRenderer.sprite = config.Sprite;

            Health = config.MaxHealth;
            DistanceTravelled = 0f;
            _laneOffset = _targetLaneOffset = Mathf.Clamp(laneOffset, -_path.LaneSpread, _path.LaneSpread);
            _avoidanceTimer = 0f;
            transform.position = _path.GetPoint(0, _laneOffset);
            transform.rotation = Quaternion.identity;
            _spriteRenderer.flipX = false;
            _nextWaypointIndex = 1;
            _isAlive = true;

            UpdateFacing(instant: true);
        }

        private void Update()
        {
            if (!_isAlive) return;

            UpdateLane();
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
            Vector2 direction = _path.GetPoint(_nextWaypointIndex, _laneOffset) - transform.position;
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
                Vector3 target = _path.GetPoint(_nextWaypointIndex, _laneOffset);
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

        /// <summary>
        /// Walk side by side instead of through each other: if a slower enemy is just ahead on (almost) the same lane,
        /// move sideways to the freer side of the road. Speed is never changed — only the lane.
        /// </summary>
        private void UpdateLane()
        {
            _avoidanceTimer -= Time.deltaTime;
            if (_avoidanceTimer <= 0f)
            {
                _avoidanceTimer = _avoidanceInterval;
                ChooseLaneAroundSlowerEnemies();
            }

            _laneOffset = Mathf.MoveTowards(_laneOffset, _targetLaneOffset, _laneChangeSpeed * Time.deltaTime);
        }

        private void ChooseLaneAroundSlowerEnemies()
        {
            float spread = _path.LaneSpread;
            if (spread <= 0f || _nextWaypointIndex >= _path.Count) return;

            // Measured geometrically (not by DistanceTravelled), so it also works where two roads merge.
            Vector2 forward = _path.GetPoint(_nextWaypointIndex, _laneOffset) - transform.position;
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();
            var leftSide = new Vector2(-forward.y, forward.x);

            _neighbours.Clear();
            Physics2D.OverlapCircle(transform.position, _lookAhead, _crowdFilter, _neighbours);

            foreach (Collider2D hit in _neighbours)
            {
                Enemy other = hit.GetComponentInParent<Enemy>();
                if (other == null || other == this || !other.IsAlive) continue;

                Vector2 delta = other.transform.position - transform.position;
                float ahead = Vector2.Dot(delta, forward);
                if (ahead <= 0f || ahead > _lookAhead) continue;           // only enemies in front of us
                if (other.CurrentSpeed >= CurrentSpeed) continue;          // we are not catching up with it

                float otherLane = _laneOffset + Vector2.Dot(delta, leftSide); // its position in our lane coordinates
                if (Mathf.Abs(_targetLaneOffset - otherLane) >= _minLaneGap) continue; // already beside it

                float left = otherLane + _minLaneGap;
                float right = otherLane - _minLaneGap;
                bool leftFits = left <= spread;
                bool rightFits = right >= -spread;
                bool preferLeft = _targetLaneOffset >= otherLane;

                if (leftFits && (preferLeft || !rightFits)) _targetLaneOffset = left;
                else if (rightFits) _targetLaneOffset = right;
                else _targetLaneOffset = otherLane >= 0f ? -spread : spread; // road too narrow: use the far edge
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

        private void OnSpeedModifiersChanged(float value) => SpeedMultiplierChanged?.Invoke(value);

        // ISlowable
        public void AddSpeedModifier(object source, float multiplier) => _speedModifiers.Set(source, multiplier);
        public void RemoveSpeedModifier(object source) => _speedModifiers.Remove(source);
    }
}
