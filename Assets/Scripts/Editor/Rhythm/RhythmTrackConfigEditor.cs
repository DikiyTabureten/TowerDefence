using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>
    /// Inspector of a Rhythm Track: the fields, three ways to fill the notes
    /// (Bake from Pattern / Import MIDI… / Analyze Audio Clip…) and a timeline preview of one loop.
    /// </summary>
    [CustomEditor(typeof(RhythmTrackConfig))]
    internal sealed class RhythmTrackConfigEditor : UnityEditor.Editor
    {
        private const int BeatsPerBarPreview = 4;
        private const float TimelineHeight = 64f;

        private static readonly Color TimelineBackground = new Color(0.13f, 0.14f, 0.16f);
        private static readonly Color BeatColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color BarColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color NoteColor = new Color32(0xFF, 0xC8, 0x3D, 0xFF);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var config = (RhythmTrackConfig)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Fill the notes", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Bake from Pattern", "Turn Pattern + Steps Per Beat into notes.")))
                    BakeFromPattern(config);
                if (GUILayout.Button(new GUIContent("Import MIDI…", "Read notes from a .mid file.")))
                    MidiImportWindow.ImportFromFile(config);
                if (GUILayout.Button(new GUIContent("Analyze Audio Clip…", "Find notes in the Clip (for songs without MIDI).")))
                    AudioAnalysisWindow.Open(config);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview (one loop)", EditorStyles.boldLabel);
            DrawTimeline(config);
        }

        /// <summary>x = note, . or - = rest; spaces and | are ignored. Loop = steps / Steps Per Beat.</summary>
        public static bool BakeFromPattern(RhythmTrackConfig config)
        {
            var steps = new List<char>();
            foreach (char c in config.Pattern ?? string.Empty)
                if (c == 'x' || c == 'X' || c == '.' || c == '-') steps.Add(c);

            int stepsPerBeat = Mathf.Max(1, config.StepsPerBeat);
            var notes = new List<RhythmNote>();
            for (int i = 0; i < steps.Count; i++)
                if (steps[i] == 'x' || steps[i] == 'X')
                    notes.Add(new RhythmNote((float)i / stepsPerBeat));

            if (notes.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake from Pattern", "The pattern has no notes. Use x for a note and . for a rest, e.g. x.x.x.xxx.x.x.x.", "OK");
                return false;
            }

            float loopBeats = (float)steps.Count / stepsPerBeat;
            string source = $"Pattern \"{config.Pattern}\" ({steps.Count} steps, {stepsPerBeat} per beat), baked {DateTime.Now:yyyy-MM-dd HH:mm}";

            Undo.RecordObject(config, "Bake Rhythm Pattern");
            config.SetNotes(notes, config.Bpm, loopBeats, config.FirstNoteOffset, source);
            EditorUtility.SetDirty(config);
            return true;
        }

        private static void DrawTimeline(RhythmTrackConfig config)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, TimelineHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, TimelineBackground);

            float loopBeats = config.LoopLengthBeats;
            if (loopBeats <= 0f)
            {
                EditorGUI.LabelField(rect, "No loop length: set Loop Length Beats or assign a Clip.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Beats and bars
            int beatCount = Mathf.FloorToInt(loopBeats);
            int beatStep = Mathf.Max(1, Mathf.CeilToInt(beatCount / (rect.width / 4f))); // keep at least 4 px between lines
            for (int beat = 0; beat <= beatCount; beat += beatStep)
            {
                bool isBar = beat % BeatsPerBarPreview == 0;
                float x = rect.x + beat / loopBeats * rect.width;
                EditorGUI.DrawRect(new Rect(x, rect.y, isBar ? 2f : 1f, rect.height), isBar ? BarColor : BeatColor);
            }

            // Notes: height ~ velocity
            foreach (RhythmNote note in config.Notes)
            {
                if (note.Beat < 0f || note.Beat >= loopBeats) continue;
                float x = rect.x + note.Beat / loopBeats * rect.width;
                float height = rect.height * (0.25f + 0.75f * Mathf.Clamp01(note.Velocity));
                EditorGUI.DrawRect(new Rect(x - 1f, rect.yMax - height, 2f, height), NoteColor);
            }

            EditorGUILayout.LabelField(
                $"{config.Bpm:0.##} BPM · {config.Notes.Count} notes · {config.NotesPerSecond:0.00} notes/s · " +
                $"loop {loopBeats:0.##} beats ({config.LoopLengthSeconds:0.00} s)" +
                (config.LoopLengthBeatsSetting <= 0f ? " from clip" : ""),
                EditorStyles.miniLabel);
        }
    }
}
