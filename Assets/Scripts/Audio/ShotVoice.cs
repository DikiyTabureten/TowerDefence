using UnityEngine;

namespace TowerDefense
{
    /// <summary>One pooled AudioSource of ShotNotePlayer. Knows when its scheduled sound ends (in dsp time).</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ShotVoice : MonoBehaviour, IPoolable
    {
        private AudioSource _source;

        public AudioSource Source => _source != null ? _source : _source = GetComponent<AudioSource>();

        /// <summary>dsp time when the scheduled sound has finished playing.</summary>
        public double EndDsp { get; set; }

        private void Awake()
        {
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 0f;
        }

        // IPoolable
        public void OnRent() { }

        public void OnReturn()
        {
            Source.Stop();
            Source.clip = null;
            EndDsp = 0.0;
        }
    }
}
