using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Visual-only: plays the walk cycle from the enemy's config based on distance moved,
    /// so slowed enemies walk slower and stopped ones stand still. Also toggles dust
    /// for configs with "Emits Dust". Works with the single shared Enemy prefab.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyWalkAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Tooltip("Optional child Particle System. Emits only for configs with Emits Dust.")]
        private ParticleSystem _dust;

        private Enemy _enemy;
        private EnemyConfig _config;
        private Vector3 _lastPosition;
        private float _cycle;   // 0..1 progress through the walk cycle

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            SetDust(false);
        }

        private void LateUpdate()
        {
            EnemyConfig config = _enemy.Config;
            if (config == null) return;               // Init not called yet

            if (config != _config) ApplyConfig(config); // first frame after Init (or a reused object)

            float moved = Vector2.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            var frames = config.WalkFrames;
            if (frames == null || frames.Count == 0) return;

            _cycle = Mathf.Repeat(_cycle + moved / config.DistancePerCycle, 1f);
            int index = Mathf.FloorToInt(_cycle * frames.Count) % frames.Count;
            if (frames[index] != null) _renderer.sprite = frames[index];
        }

        private void ApplyConfig(EnemyConfig config)
        {
            _config = config;
            _lastPosition = transform.position;
            _cycle = Random.value;                    // desync so a crowd doesn't march in lockstep
            SetDust(config.EmitsDust);
        }

        private void SetDust(bool isOn)
        {
            if (_dust == null) return;
            var emission = _dust.emission;
            emission.enabled = isOn;
        }
    }
}
