using UnityEngine;

namespace TowerDefense
{
    /// <summary>Audio-only: plays the cannon shot every time ShooterTower fires. Holds no gameplay logic.</summary>
    [RequireComponent(typeof(ShooterTower), typeof(AudioSource))]
    public sealed class CannonSound : MonoBehaviour
    {
        [SerializeField] private AudioClip _shot;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField, Range(0f, 0.3f), Tooltip("Random pitch spread so many cannons don't sound identical.")]
        private float _pitchVariation = 0.08f;

        private ShooterTower _tower;
        private AudioSource _source;

        private void Awake()
        {
            _tower = GetComponent<ShooterTower>();
            _source = GetComponent<AudioSource>();
        }

        private void OnEnable() => _tower.Fired += OnFired;
        private void OnDisable() => _tower.Fired -= OnFired;

        private void OnFired(Enemy target)
        {
            if (_shot == null) return;
            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _source.PlayOneShot(_shot, _volume);
        }
    }
}
