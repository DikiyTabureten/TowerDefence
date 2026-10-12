using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Visual-only: trumpeters sway, notes float out. Plays more notes while the aura
    /// is buffing towers, and a burst whenever a new tower enters the aura.
    /// </summary>
    public sealed class OrchestraAnimator : MonoBehaviour
    {
        [Header("Sway")]
        [SerializeField] private Transform[] _trumpeters;      // the 3 child trumpeter sprites
        [SerializeField] private float _swayAngle = 8f;        // degrees left/right
        [SerializeField] private float _swaySpeed = 4f;        // higher = faster rhythm

        [Header("Notes")]
        [SerializeField] private ParticleSystem _notes;        // child Particle System
        [SerializeField, Min(0f)] private float _idleNoteRate = 1.5f;    // notes/sec with no towers buffed
        [SerializeField, Min(0f)] private float _activeNoteRate = 4f;    // notes/sec while buffing
        [SerializeField] private int _pulseBurstCount = 6;     // burst when a new tower gets the buff

        private IAuraTower _aura;
        private float[] _baseAngles;
        private int _lastCount;

        private void Awake()
        {
            _aura = GetComponent<IAuraTower>();
            if (_aura == null)
                Debug.LogError("OrchestraAnimator needs an aura tower (e.g. AttackSpeedAuraTower) on the same object.", this);

            _baseAngles = new float[_trumpeters.Length];
            for (int i = 0; i < _trumpeters.Length; i++)
                _baseAngles[i] = _trumpeters[i].localEulerAngles.z;
        }

        private void OnEnable()
        {
            if (_aura == null) return;
            _aura.AffectedCountChanged += OnAffectedCountChanged;
            _lastCount = _aura.AffectedCount;
            SetNoteRate(_lastCount);
        }

        private void OnDisable()
        {
            if (_aura != null) _aura.AffectedCountChanged -= OnAffectedCountChanged;
        }

        private void Update()
        {
            float time = Time.time * _swaySpeed;

            for (int i = 0; i < _trumpeters.Length; i++)
            {
                // Offset each player's phase so they don't move in lockstep.
                float phase = i * (2f * Mathf.PI / _trumpeters.Length);
                float angle = _baseAngles[i] + Mathf.Sin(time + phase) * _swayAngle;
                _trumpeters[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void OnAffectedCountChanged(int count)
        {
            if (count > _lastCount) PlayPulse();
            _lastCount = count;
            SetNoteRate(count);
        }

        public void PlayPulse()
        {
            if (_notes != null) _notes.Emit(_pulseBurstCount);
        }

        private void SetNoteRate(int affectedCount)
        {
            if (_notes == null) return;
            var emission = _notes.emission;
            emission.rateOverTime = affectedCount > 0 ? _activeNoteRate : _idleNoteRate;
        }
    }
}
