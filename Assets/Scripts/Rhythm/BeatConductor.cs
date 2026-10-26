using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Plays a RhythmTrackConfig on a loop and raises events for its notes.
    /// Time comes only from AudioSettings.dspTime (the audio clock), never from Time.time.
    /// NoteUpcoming fires LookAhead seconds before a note with its exact dsp time, so sounds can be scheduled
    /// precisely; NoteHit fires when the note's time has come. Both use the same note numbers.
    /// The music clip is optional: without it the conductor still runs on the audio clock
    /// ("only the cannons play the melody").
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BeatConductor : MonoBehaviour
    {
        [SerializeField] private RhythmTrackConfig _track;
        [SerializeField, Min(0f), Tooltip("Seconds between pressing Play and beat 0 (lets the audio system prepare).")]
        private float _startDelay = 0.1f;
        [SerializeField, Min(0f), Tooltip("How many seconds before a note NoteUpcoming is raised (sounds are scheduled this far ahead).")]
        private float _lookAhead = 0.1f;
        [SerializeField, Tooltip("Log every note to the Console (debugging only).")]
        private bool _debugLog;

        /// <summary>Walks through the notes loop after loop; one for NoteUpcoming, one for NoteHit.</summary>
        private struct Cursor
        {
            public int Cycle;
            public int Index;
            public int Global;
        }

        private AudioSource _source;
        private RhythmNote[] _notes;     // notes inside one loop, sorted by beat
        private double _loopBeats;
        private double _startDsp;        // dspTime of the clip start (beat 0 is FirstNoteOffset later)
        private double _pausedAtDsp;
        private Cursor _upcoming;
        private Cursor _hit;
        private bool _isRunning;
        private bool _isPaused;

        /// <summary>Raised LookAhead seconds before a note: (global note number, note, exact dsp time of the note).</summary>
        public event Action<int, RhythmNote, double> NoteUpcoming;

        /// <summary>Raised when a note's time has come: (global note number across all loops, note).</summary>
        public event Action<int, RhythmNote> NoteHit;

        public RhythmTrackConfig Track => _track;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            if (_track == null || _track.Notes.Count == 0)
            {
                Debug.LogError("BeatConductor needs a Rhythm Track with at least one note.", this);
                enabled = false;
                return;
            }

            _loopBeats = _track.LoopLengthBeats;
            if (_loopBeats <= 0.0)
            {
                Debug.LogError($"Rhythm Track '{_track.name}' has no loop length: set Loop Length Beats (it has no clip to measure).", this);
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

            _source.playOnAwake = false;
            if (_track.Clip != null)
            {
                _source.clip = _track.Clip;
                _source.loop = true;
            }
        }

        private void Start()
        {
            _startDsp = AudioSettings.dspTime + _startDelay;
            if (_track.Clip != null) _source.PlayScheduled(_startDsp);
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning || _isPaused) return;

            double now = AudioSettings.dspTime;

            // While loops: a long frame still raises every note that has passed, each exactly once.
            while (NoteDsp(_upcoming) - now <= _lookAhead)
            {
                RhythmNote note = _notes[_upcoming.Index];
                NoteUpcoming?.Invoke(_upcoming.Global, note, NoteDsp(_upcoming));
                Advance(ref _upcoming);
            }

            while (NoteDsp(_hit) <= now)
            {
                RhythmNote note = _notes[_hit.Index];
                if (_debugLog) Debug.Log($"[BeatConductor] note #{_hit.Global} (loop {_hit.Cycle}, beat {note.Beat:0.###}, pitch {note.Pitch})", this);
                NoteHit?.Invoke(_hit.Global, note);
                Advance(ref _hit);
            }
        }

        /// <summary>Exact dsp time of the note a cursor points to.</summary>
        private double NoteDsp(Cursor cursor)
        {
            double beats = cursor.Cycle * _loopBeats + _notes[cursor.Index].Beat;
            return _startDsp + _track.FirstNoteOffset + beats * _track.SecondsPerBeat;
        }

        private void Advance(ref Cursor cursor)
        {
            cursor.Global++;
            cursor.Index++;
            if (cursor.Index >= _notes.Length)
            {
                cursor.Index = 0;
                cursor.Cycle++;
            }
        }

        /// <summary>Stops the music and the notes; Resume continues exactly where it stopped.</summary>
        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _isPaused = true;
            _pausedAtDsp = AudioSettings.dspTime;
            if (_track.Clip != null) _source.Pause();
        }

        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            // Shift the start by the paused time, so the beat grid continues where it stopped.
            double now = AudioSettings.dspTime;
            _startDsp += now - _pausedAtDsp;
            _isPaused = false;

            if (_track.Clip == null) return;
            if (now < _startDsp) _source.PlayScheduled(_startDsp); // paused before the music had started
            else _source.UnPause();
        }
    }
}
