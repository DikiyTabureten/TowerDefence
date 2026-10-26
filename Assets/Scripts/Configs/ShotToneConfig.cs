using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>A tonal sample and the MIDI note it was recorded at.</summary>
    [Serializable]
    public struct ToneSample
    {
        public AudioClip Clip;
        [Tooltip("MIDI note the clip was recorded at (C4 = 60).")]
        public int RootMidi;
    }

    /// <summary>
    /// How a cannon shot sounds when the cannons play the melody: a tonal layer pitched to the note
    /// plus an unpitched "boom". Read by ShotNotePlayer.
    /// </summary>
    [CreateAssetMenu(fileName = "ShotTone", menuName = "Tower Defense/Shot Tone")]
    public sealed class ShotToneConfig : ScriptableObject
    {
        [Header("Sounds")]
        [SerializeField, Tooltip("Usually one sample; several on different notes = multisampling (the closest root is used).")]
        private List<ToneSample> _samples = new List<ToneSample>();
        [SerializeField, Tooltip("Unpitched boom of the shot. Its pitch never changes.")]
        private AudioClip _boomClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _toneVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _boomVolume = 0.6f;
        [SerializeField, Min(0f), Tooltip("How much louder a note gets when several cannons play it at once: × (1 + gain · log2(count)).")]
        private float _stackGain = 0.3f;

        [Header("Pitch")]
        [SerializeField, Tooltip("Notes below are moved up an octave until they fit.")]
        private int _minMidi = 48;
        [SerializeField, Tooltip("Notes above are moved down an octave until they fit.")]
        private int _maxMidi = 72;
        [SerializeField, Tooltip("Shift the whole melody by this many semitones.")]
        private int _transposeSemitones;
        [SerializeField, Tooltip("Note to play when the rhythm note has no pitch (pattern or audio analysis).")]
        private int _unpitchedMidi = 60;

        [Header("Voices")]
        [SerializeField, Min(2), Tooltip("Size of the AudioSource pool. Each note uses up to two (tone + boom).")]
        private int _maxVoices = 16;

        public IReadOnlyList<ToneSample> Samples => _samples;
        public AudioClip BoomClip => _boomClip;
        public float ToneVolume => _toneVolume;
        public float BoomVolume => _boomVolume;
        public float StackGain => _stackGain;
        public int MinMidi => _minMidi;
        public int MaxMidi => _maxMidi;
        public int TransposeSemitones => _transposeSemitones;
        public int UnpitchedMidi => _unpitchedMidi;
        public int MaxVoices => _maxVoices;
    }
}
