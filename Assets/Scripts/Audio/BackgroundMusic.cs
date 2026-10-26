using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Music volume gate. Every frame it asks one question — "is at least one War Orchestra on the map?" —
    /// and fades the music in while the answer is yes, out when it is no.
    /// It never stops the AudioSource: the BeatConductor on the same object keeps playing the rhythm track
    /// from the start of the level (the cannons need its beat), this component only decides whether it is heard.
    /// Five orchestras still mean one AudioSource and one copy of the music.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Seconds to fade the music in or out.")]
        private float _fadeTime = 0.6f;

        private AudioSource _source;
        private Transform _towersRoot;
        private float _volume;           // the AudioSource's volume set in the Inspector = "music on" volume
        private bool _drivenByConductor;

        /// <summary>The flag: true while the map has at least one orchestra.</summary>
        public bool ShouldPlay { get; private set; }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _volume = _source.volume;
            _source.volume = 0f;
            _drivenByConductor = GetComponent<BeatConductor>() != null;
            if (!_drivenByConductor)
            {
                _source.loop = true;
                _source.playOnAwake = false;
            }
        }

        /// <summary>Called by Bootstrap: towers are created under this parent by TowerFactory.</summary>
        public void Init(Transform towersRoot) => _towersRoot = towersRoot;

        private void Update()
        {
            if (_towersRoot == null) return;

            ShouldPlay = _towersRoot.GetComponentInChildren<AttackSpeedAuraTower>() != null;

            // Without a BeatConductor nobody else plays the clip, so start it here.
            if (!_drivenByConductor && ShouldPlay && !_source.isPlaying) _source.Play();

            float target = ShouldPlay ? _volume : 0f;
            float step = _fadeTime > 0f ? _volume / _fadeTime * Time.unscaledDeltaTime : 1f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, step);
        }
    }
}
