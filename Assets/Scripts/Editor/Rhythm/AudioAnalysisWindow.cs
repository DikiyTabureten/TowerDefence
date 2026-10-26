using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>
    /// Finds notes in a song that has no MIDI: detects onsets in the RhythmTrackConfig's clip,
    /// estimates the BPM, optionally quantizes to a beat grid, and writes the notes into the config.
    /// </summary>
    internal sealed class AudioAnalysisWindow : EditorWindow
    {
        private static readonly string[] GridLabels = { "1 beat", "1/2 beat", "1/4 beat", "1/8 beat" };
        private static readonly float[] GridBeats = { 1f, 0.5f, 0.25f, 0.125f };

        private RhythmTrackConfig _config;
        private float _sensitivity = 1.5f;
        private float _minGapSeconds = 0.12f;
        private float _bpm = 120f;
        private bool _quantize = true;
        private int _gridIndex = 2;

        private AudioOnsetAnalyzer.Flux _flux;   // cached per clip: reading + FFT is the slow part
        private AudioClip _fluxClip;
        private List<Onset> _onsets;
        private List<RhythmNote> _preview;
        private string _error;

        public static void Open(RhythmTrackConfig config)
        {
            var window = GetWindow<AudioAnalysisWindow>(true, "Analyze Audio Clip");
            window._config = config;
            window._bpm = config.Bpm;
            window._preview = null;
            window._error = null;
            window.minSize = new Vector2(380f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("Select a Rhythm Track and press Analyze Audio Clip in its Inspector.", MessageType.Info);
                return;
            }

            AudioClip clip = _config.Clip;
            EditorGUILayout.LabelField("Track", _config.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Clip", clip != null ? $"{clip.name} ({clip.length:0.0} s)" : "none");
            if (clip == null)
            {
                EditorGUILayout.HelpBox("Assign an Audio Clip to the Rhythm Track first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            _sensitivity = EditorGUILayout.Slider(
                new GUIContent("Sensitivity", "Threshold = local average × this. Higher = only strong hits, fewer notes."),
                _sensitivity, 0.8f, 4f);
            _minGapSeconds = EditorGUILayout.Slider(
                new GUIContent("Min Gap (s)", "Two notes are never closer than this."), _minGapSeconds, 0.03f, 1f);

            using (new EditorGUILayout.HorizontalScope())
            {
                _bpm = Mathf.Max(1f, EditorGUILayout.FloatField("BPM", _bpm));
                if (GUILayout.Button(new GUIContent("Estimate", "Guess the tempo (60–180 BPM) from the song."), GUILayout.Width(80f)))
                    Run(() => _bpm = AudioOnsetAnalyzer.EstimateBpm(GetFlux(clip)));
            }

            _quantize = EditorGUILayout.Toggle(new GUIContent("Quantize", "Snap notes to the nearest grid step and remove duplicates."), _quantize);
            using (new EditorGUI.DisabledScope(!_quantize))
                _gridIndex = EditorGUILayout.Popup("Grid", _gridIndex, GridLabels);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Analyze", GUILayout.Height(24f))) Run(Analyze);
                using (new EditorGUI.DisabledScope(_preview == null || _preview.Count == 0))
                    if (GUILayout.Button("Apply", GUILayout.Height(24f))) Apply(clip);
            }

            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);

            if (_preview != null)
            {
                float loopSeconds = clip.length;
                EditorGUILayout.HelpBox(
                    $"Onsets found: {_onsets.Count}\nNotes after quantizing: {_preview.Count}\n" +
                    $"Notes per second: {(loopSeconds > 0f ? _preview.Count / loopSeconds : 0f):0.00}",
                    MessageType.Info);
            }
        }

        private AudioOnsetAnalyzer.Flux GetFlux(AudioClip clip)
        {
            if (_flux != null && _fluxClip == clip) return _flux;

            EditorUtility.DisplayProgressBar("Analyze Audio Clip", "Reading samples and computing the spectrum…", 0.3f);
            try
            {
                float[] mono = AudioOnsetAnalyzer.ReadMono(clip, out int sampleRate);
                _flux = AudioOnsetAnalyzer.ComputeFlux(mono, sampleRate);
                _fluxClip = clip;
                return _flux;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void Analyze()
        {
            AudioClip clip = _config.Clip;
            AudioOnsetAnalyzer.Flux flux = GetFlux(clip);
            _onsets = AudioOnsetAnalyzer.PickOnsets(flux, _sensitivity, _minGapSeconds);
            float loopBeats = flux.ClipLength / (60f / _bpm);
            _preview = AudioOnsetAnalyzer.ToNotes(_onsets, _bpm, loopBeats, _quantize, GridBeats[_gridIndex]);
        }

        private void Apply(AudioClip clip)
        {
            float loopBeats = clip.length / (60f / _bpm);
            string grid = _quantize ? $", quantized to {GridLabels[_gridIndex]}" : "";
            string source = $"Audio analysis: {clip.name}, sensitivity {_sensitivity:0.##}, min gap {_minGapSeconds:0.###} s{grid}, " +
                            $"{DateTime.Now:yyyy-MM-dd HH:mm}";

            Undo.RecordObject(_config, "Analyze Audio Clip");
            _config.SetNotes(_preview, _bpm, loopBeats, 0f, source);
            EditorUtility.SetDirty(_config);
            Debug.Log($"[Rhythm] Applied {_preview.Count} notes from audio analysis at {_bpm:0.#} BPM.", _config);
        }

        private void Run(Action action)
        {
            _error = null;
            try
            {
                action();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception);
            }

            // The analysis may reimport the clip; stop this OnGUI pass and redraw cleanly.
            GUIUtility.ExitGUI();
        }
    }
}
