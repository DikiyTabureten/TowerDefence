using System.Collections;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Visual-only: the Great Bell tolls rhythmically while its slow aura affects enemies.
    /// The slow itself is applied continuously by SlowAuraTower; this only shows it.
    /// </summary>
    public sealed class BellAnimator : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private Transform _bellBody;           // swings under the headstock
        [SerializeField] private Transform _towerVisual;        // parent of bell parts, gets a small "punch"

        [Header("Rhythm")]
        [SerializeField, Min(0.3f), Tooltip("Seconds between tolls while enemies are slowed.")]
        private float _tollInterval = 1.6f;

        [Header("Swing")]
        [SerializeField] private float _swingDistance = 0.07f;  // units the bell mouth moves
        [SerializeField] private float _swingFrequency = 3.2f;  // swings per second
        [SerializeField] private float _swingDamping = 2.8f;    // higher = settles faster
        [SerializeField] private float _swingDuration = 1.2f;
        [SerializeField] private float _squash = 0.06f;

        [Header("Sound waves")]
        [SerializeField] private SpriteRenderer[] _waves;       // 2 child sprites using bell_ringwave
        [SerializeField] private float _waveDelay = 0.12f;      // gap between the waves
        [SerializeField] private float _waveDuration = 0.7f;
        [SerializeField] private float _waveSpriteRadius = 0.45f; // ring radius of the sprite at scale 1

        private IAuraTower _aura;
        private Vector3 _bellRestPos;
        private Coroutine _swing;
        private float _tollTimer;

        private void Awake()
        {
            _aura = GetComponent<IAuraTower>();
            if (_aura == null)
                Debug.LogError("BellAnimator needs an aura tower (e.g. SlowAuraTower) on the same object.", this);

            _bellRestPos = _bellBody.localPosition;
            foreach (var w in _waves) w.enabled = false;
        }

        private void Update()
        {
            if (_aura == null) return;

            if (_aura.AffectedCount == 0)
            {
                _tollTimer = 0f;       // the next enemy that walks in triggers a toll immediately
                return;
            }

            _tollTimer -= Time.deltaTime;
            if (_tollTimer > 0f) return;

            _tollTimer = _tollInterval;
            PlayToll();
        }

        public void PlayToll()
        {
            if (_swing != null) StopCoroutine(_swing);
            _swing = StartCoroutine(SwingRoutine());

            for (int i = 0; i < _waves.Length; i++)
                StartCoroutine(WaveRoutine(_waves[i], i * _waveDelay, i == 0 ? 1f : 0.6f));
        }

        private IEnumerator SwingRoutine()
        {
            for (float t = 0f; t < _swingDuration; t += Time.deltaTime)
            {
                float decay = Mathf.Exp(-t * _swingDamping);
                float wave = Mathf.Sin(t * _swingFrequency * 2f * Mathf.PI);

                // Top-down swing: the bell mouth slides across the beam's axis and looks shorter at the extremes.
                _bellBody.localPosition = _bellRestPos + Vector3.up * (wave * _swingDistance * decay);
                _bellBody.localScale = new Vector3(1f, 1f - _squash * Mathf.Abs(wave) * decay, 1f);

                if (_towerVisual != null)
                    _towerVisual.localScale = Vector3.one * (1f + 0.05f * Mathf.Exp(-t * 18f));
                yield return null;
            }
            _bellBody.localPosition = _bellRestPos;
            _bellBody.localScale = Vector3.one;
            if (_towerVisual != null) _towerVisual.localScale = Vector3.one;
            _swing = null;
        }

        private IEnumerator WaveRoutine(SpriteRenderer wave, float delay, float strength)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            wave.enabled = true;
            Color c = wave.color;

            // Waves end exactly at the slow radius from the tower's config.
            float range = _aura != null ? _aura.Radius : 2f;
            float parentScale = Mathf.Max(Mathf.Abs(wave.transform.parent.lossyScale.x), 0.0001f);

            for (float t = 0f; t < _waveDuration; t += Time.deltaTime)
            {
                float q = t / _waveDuration;
                float eased = 1f - (1f - q) * (1f - q);          // fast start, slows near the edge
                wave.transform.localScale = Vector3.one * (range * eased / _waveSpriteRadius / parentScale);
                wave.color = new Color(c.r, c.g, c.b, (1f - q) * strength);
                yield return null;
            }
            wave.enabled = false;
            wave.color = c;
        }
    }
}
