using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// One looping music source for the whole level. Every frame it asks a single question —
    /// "is at least one War Orchestra on the map?" — and plays the music while the answer is yes.
    /// Five orchestras still mean one AudioSource and one copy of the music;
    /// if the last orchestra disappears, the music stops.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusic : MonoBehaviour
    {
        private AudioSource _source;
        private Transform _towersRoot;

        /// <summary>The flag: true while the map has at least one orchestra.</summary>
        public bool ShouldPlay { get; private set; }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
        }

        /// <summary>Called by Bootstrap: towers are created under this parent by TowerFactory.</summary>
        public void Init(Transform towersRoot) => _towersRoot = towersRoot;

        private void Update()
        {
            if (_towersRoot == null) return;

            ShouldPlay = _towersRoot.GetComponentInChildren<AttackSpeedAuraTower>() != null;

            if (ShouldPlay && !_source.isPlaying) _source.Play();
            else if (!ShouldPlay && _source.isPlaying) _source.Stop();
        }
    }
}
