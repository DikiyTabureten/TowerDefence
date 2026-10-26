using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Plays a RhythmTrackConfig's clip on a loop and raises NoteHit on every note.
    /// Time comes only from AudioSettings.dspTime (the audio clock), never from Time.time,
    /// so the notes stay locked to what is heard even when frames are slow or uneven.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BeatConductor : MonoBehaviour
    {
        [SerializeField] private RhythmTrackConfig _track;
        [SerializeField, Min(0f), Tooltip("Seconds between pressing Play and the music starting (lets the audio system prepare).")]
        private float _startDelay = 0.1f;
        [SerializeField, Tooltip("Log every note to the Console (debugging only).")]
        private bool _debugLog;

        private AudioSource _source;
        private RhythmNote[] _notes;     // notes inside one loop, sorted by beat
        private double _loopBeats;
        private double _startDsp;        // dspTime of beat 0 of loop 0 (minus FirstNoteOffset)
        private double _pausedAtDsp;
        private int _cycle;              // how many loops have been completed
        private int _nextNote;           // index in _notes of the next note to raise
        private int _globalNoteIndex;    // grows forever, across loops
        private bool _isRunning;
        private bool _isPaused;

        /// <summary>Raised for every note: (global note number across all loops, note).</summary>
        public event Action<int, RhythmNote> NoteHit;

        public RhythmTrackConfig Track => _track;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            if (_track == null || _track.Clip == null || _track.Notes.Count == 0)
            {
                Debug.LogError("BeatConductor needs a Rhythm Track with an audio clip and at least one note.", this);
                enabled = false;
                return;
            }

            _loopBeats = _track.LoopLengthBeats;
            if (_loopBeats <= 0.0)
            {
                Debug.LogError($"Rhythm Track '{_track.name}' has a zero loop length.", this);
                enabled = false;
                return;
            }

            // Only notes inside the loop can repeat correctly.
            var notes = new List<RhythmNote>();
            foreach (RhythmNote note in _track.Notes)
                if (note.Beat >= 0f && note.Beat < _loopBeats) notes.Add(note);
            notes.Sort((a, b) => a.Beat.CompareTo(b.Beat));
            _notes = notes.ToArray();

            if (_notes.Length == 0)
            {
                Debug.LogError($"Rhythm Track '{_track.name}' has no notes inside its loop ({_loopBeats:0.##} beats).", this);
                enabled = false;
                return;
            }

            _source.clip = _track.Clip;
            _source.loop = true;
            _source.playOnAwake = false;
        }

        private void Start()
        {
            _startDsp = AudioSettings.dspTime + _startDelay;
            _source.PlayScheduled(_startDsp);
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning || _isPaused) return;

            double songSeconds = AudioSettings.dspTime - _startDsp - _track.FirstNoteOffset;
            if (songSeconds < 0.0) return;
            double nowBeats = songSeconds / _track.SecondsPerBeat;

            // A while loop, so a long frame still raises every note that has passed.
            while (true)
            {
                if (_nextNote >= _notes.Length)
                {
                    _nextNote = 0;
                    _cycle++;
                }

                RhythmNote note = _notes[_nextNote];
                double noteBeats = _cycle * _loopBeats + note.Beat;
                if (noteBeats > nowBeats) break;

                if (_debugLog) Debug.Log($"[BeatConductor] note #{_globalNoteIndex} (loop {_cycle}, beat {note.Beat:0.###})", this);
                NoteHit?.Invoke(_globalNoteIndex, note);
                _globalNoteIndex++;
                _nextNote++;
            }
        }

        /// <summary>Stops the music and the notes; Resume continues exactly where it stopped.</summary>
        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _isPaused = true;
            _pausedAtDsp = AudioSettings.dspTime;
            _source.Pause();
        }

        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            // Shift the start by the paused time, so the beat grid continues where it stopped.
            double now = AudioSettings.dspTime;
            _startDsp += now - _pausedAtDsp;
            _isPaused = false;

            if (now < _startDsp) _source.PlayScheduled(_startDsp); // paused before the music had started
            else _source.UnPause();
        }
    }
}
