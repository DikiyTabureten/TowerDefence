using UnityEngine;

namespace TowerDefense
{
    /// <summary>Audio-only: plays the bell strike on every toll of BellAnimator, in sync with the swing.</summary>
    [RequireComponent(typeof(BellAnimator), typeof(AudioSource))]
    public sealed class BellSound : MonoBehaviour
    {
        [SerializeField] private AudioClip _toll;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;

        private BellAnimator _bell;
        private AudioSource _source;

        private void Awake()
        {
            _bell = GetComponent<BellAnimator>();
            _source = GetComponent<AudioSource>();
        }

        private void OnEnable() => _bell.Tolled += OnTolled;
        private void OnDisable() => _bell.Tolled -= OnTolled;

        private void OnTolled()
        {
            if (_toll != null) _source.PlayOneShot(_toll, _volume);
        }
    }
}
