using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>
    /// Shows the tracks/channels of a MIDI file and imports the chosen one into a RhythmTrackConfig.
    /// Only note starts are used; notes starting within 1/32 of a beat of each other merge into one (the highest).
    /// </summary>
    internal sealed class MidiImportWindow : EditorWindow
    {
        private const float ChordMergeBeats = 1f / 32f;

        private RhythmTrackConfig _config;
        private MidiFile _file;
        private string _fileName;
        private int _selected;
        private Vector2 _scroll;

        public static void ImportFromFile(RhythmTrackConfig config)
        {
            string path = EditorUtility.OpenFilePanel("Import MIDI", "", "mid,midi");
            if (string.IsNullOrEmpty(path)) return;

            MidiFile file;
            try
            {
                file = MidiFileReader.Read(File.ReadAllBytes(path));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Import MIDI", $"Could not read the MIDI file:\n{exception.Message}", "OK");
                return;
            }

            if (file.Parts.Count == 0)
            {
                EditorUtility.DisplayDialog("Import MIDI", "The MIDI file has no notes.", "OK");
                return;
            }

            var window = CreateInstance<MidiImportWindow>();
            window.titleContent = new GUIContent("Import MIDI");
            window._config = config;
            window._file = file;
            window._fileName = Path.GetFileName(path);
            // Default: the part with the most notes.
            window._selected = file.Parts.IndexOf(file.Parts.OrderByDescending(p => p.Notes.Count).First());
            window.minSize = new Vector2(460f, 260f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (_config == null || _file == null)
            {
                EditorGUILayout.HelpBox("Nothing to import. Close this window and press Import MIDI again.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_fileName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Format {_file.Format} · {_file.TicksPerQuarter} ticks per beat · {_file.Bpm:0.##} BPM · " +
                $"{_file.TimeSignatureNumerator}/{_file.TimeSignatureDenominator}" + (_file.HasTimeSignature ? "" : " (default)"));

            if (_file.HasTempoChanges)
                EditorGUILayout.HelpBox("The file changes tempo. Only a constant tempo is supported: the first tempo is used for the whole track.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choose a track / channel:", EditorStyles.miniBoldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _file.Parts.Count; i++)
            {
                MidiPart part = _file.Parts[i];
                string drums = part.IsDrums ? "  [Drums]" : "";
                string label = $"{part.TrackName} — channel {part.Channel + 1}{drums} — {part.Notes.Count} notes — " +
                               $"pitch {NoteName(part.MinPitch)}–{NoteName(part.MaxPitch)}";
                if (GUILayout.Toggle(_selected == i, label, EditorStyles.radioButton)) _selected = i;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(90f))) Close();
                if (GUILayout.Button("Import", GUILayout.Width(90f)))
                {
                    Import(_file.Parts[_selected]);
                    Close();
                }
            }
        }

        private void Import(MidiPart part)
        {
            int tpq = _file.TicksPerQuarter;
            var notes = new List<RhythmNote>();
            long mergeTicks = Math.Max(1, (long)Math.Round(tpq * ChordMergeBeats));

            long groupStart = long.MinValue;
            foreach (MidiNote note in part.Notes.OrderBy(n => n.StartTick).ThenByDescending(n => n.Pitch))
            {
                if (groupStart != long.MinValue && note.StartTick - groupStart < mergeTicks)
                    continue; // chord: the highest note of the group is already kept (sorted by pitch, descending)

                groupStart = note.StartTick;
                notes.Add(new RhythmNote((float)note.StartTick / tpq, note.Pitch, note.Velocity / 127f));
            }

            // Loop = end of the last note, rounded up to a whole bar.
            float lastEndBeats = part.Notes.Max(n => Math.Max(n.EndTick, n.StartTick)) / (float)tpq;
            float bar = _file.BeatsPerBar;
            float loopBeats = Mathf.Max(bar, Mathf.Ceil(lastEndBeats / bar - 0.0001f) * bar);

            string source = $"MIDI: {_fileName}, \"{part.TrackName}\" channel {part.Channel + 1}" +
                            $"{(part.IsDrums ? " (drums)" : "")}, imported {DateTime.Now:yyyy-MM-dd HH:mm}";

            Undo.RecordObject(_config, "Import MIDI");
            _config.SetNotes(notes, _file.Bpm, loopBeats, 0f, source);
            EditorUtility.SetDirty(_config);
            Debug.Log($"[Rhythm] Imported {notes.Count} notes from {_fileName} at {_file.Bpm:0.##} BPM, loop {loopBeats} beats.", _config);
        }

        private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private static string NoteName(int pitch) => $"{Names[pitch % 12]}{pitch / 12 - 1}";
    }
}
