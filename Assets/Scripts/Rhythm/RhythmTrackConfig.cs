using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// A melody for the cannons to shoot to: the music clip, its tempo and the notes (in beats).
    /// Notes are filled by the editor tools (pattern, MIDI import, audio analysis) — see the custom inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "RhythmTrack", menuName = "Tower Defense/Rhythm Track")]
    public sealed class RhythmTrackConfig : ScriptableObject
    {
        [Header("Music")]
        [SerializeField, Tooltip("Music that plays in the level.")]
        private AudioClip _clip;
        [SerializeField, Min(1f)] private float _bpm = 120f;
        [SerializeField, Min(0f), Tooltip("Seconds from the start of the clip to beat 0.")]
        private float _firstNoteOffset;
        [SerializeField, Min(0f), Tooltip("Length of one loop in beats. 0 = take it from the clip's length.")]
        private float _loopLengthBeats;

        [Header("Notes (sorted by time)")]
        [SerializeField] private List<RhythmNote> _notes = new List<RhythmNote>();
        [SerializeField, TextArea(1, 3), Tooltip("Where the notes came from (file, track, date). Display only.")]
        private string _sourceInfo;

        [Header("Manual mode")]
        [SerializeField, Tooltip("x = note, . = rest. Spaces and | are ignored (use them to group bars).")]
        private string _pattern = "x.x.x.xxx.x.x.x.";
        [SerializeField, Min(1), Tooltip("Pattern steps per beat: 4 = sixteenth notes.")]
        private int _stepsPerBeat = 4;

        public AudioClip Clip => _clip;
        public float Bpm => _bpm;
        public float FirstNoteOffset => _firstNoteOffset;
        public IReadOnlyList<RhythmNote> Notes => _notes;
        public string SourceInfo => _sourceInfo;
        public string Pattern => _pattern;
        public int StepsPerBeat => _stepsPerBeat;

        public float SecondsPerBeat => 60f / Mathf.Max(1f, _bpm);

        /// <summary>Loop length in beats: the set value, or the clip's length when it is 0.</summary>
        public float LoopLengthBeats =>
            _loopLengthBeats > 0f ? _loopLengthBeats
            : _clip != null ? _clip.length / SecondsPerBeat
            : 0f;

        /// <summary>Raw field value (0 = from the clip), for the editor.</summary>
        public float LoopLengthBeatsSetting => _loopLengthBeats;

        public float LoopLengthSeconds => LoopLengthBeats * SecondsPerBeat;

        /// <summary>Average notes per second over one loop.</summary>
        public float NotesPerSecond => LoopLengthSeconds > 0f ? _notes.Count / LoopLengthSeconds : 0f;

        /// <summary>
        /// Replaces the notes (sorted by beat) and the timing. Used by the editor tools;
        /// they record Undo and mark the asset dirty themselves.
        /// </summary>
        public void SetNotes(IEnumerable<RhythmNote> notes, float bpm, float loopLengthBeats, float firstNoteOffset, string sourceInfo)
        {
            _notes = new List<RhythmNote>(notes);
            _notes.Sort((a, b) => a.Beat.CompareTo(b.Beat));
            _bpm = Mathf.Max(1f, bpm);
            _loopLengthBeats = Mathf.Max(0f, loopLengthBeats);
            _firstNoteOffset = Mathf.Max(0f, firstNoteOffset);
            _sourceInfo = sourceInfo;
        }
    }
}
