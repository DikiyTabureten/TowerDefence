using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>One note of a rhythm track. Time is in beats, so changing the BPM never breaks the rhythm.</summary>
    [Serializable]
    public struct RhythmNote
    {
        [Tooltip("Time in beats from the start of the loop (beat 0 = FirstNoteOffset seconds into the clip).")]
        public float Beat;
        [Tooltip("MIDI note number, 0 if unknown.")]
        public int Pitch;
        [Range(0f, 1f)] public float Velocity;

        public RhythmNote(float beat, int pitch = 0, float velocity = 1f)
        {
            Beat = beat;
            Pitch = pitch;
            Velocity = velocity;
        }
    }
}
