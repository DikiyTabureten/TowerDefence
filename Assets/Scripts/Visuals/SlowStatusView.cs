using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Visual-only: shows a spinning blue ring under an enemy while any slow is applied.
    /// Listens to Enemy.SpeedMultiplierChanged, so it works with any number of slow sources.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class SlowStatusView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ring;          // child sprite using slow_status
        [SerializeField] private float _spinSpeed = 180f;       // degrees per second

        private Enemy _enemy;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _ring.enabled = false;
        }

        private void OnEnable()
        {
            _enemy.SpeedMultiplierChanged += OnSpeedMultiplierChanged;
            OnSpeedMultiplierChanged(_enemy.SpeedMultiplier);
        }

        private void OnDisable() => _enemy.SpeedMultiplierChanged -= OnSpeedMultiplierChanged;

        private void OnSpeedMultiplierChanged(float multiplier) => _ring.enabled = multiplier < 0.999f;

        private void Update()
        {
            if (_ring.enabled)
                _ring.transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
