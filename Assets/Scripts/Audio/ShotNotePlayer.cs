using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Plays cannon shots as notes of the melody: a tonal layer pitched to the note plus an unpitched boom,
    /// scheduled at the exact dsp time of the note. One sound per note: when several cannons shoot on the
    /// same note they share it, and it only gets louder. AudioSources come from a pool (ObjectPool&lt;ShotVoice&gt;).
    /// </summary>
    public sealed class ShotNotePlayer : MonoBehaviour, IShotNoteSink
    {
        [SerializeField] private ShotToneConfig _config;
        [SerializeField, Tooltip("Log every scheduled note to the Console (debugging only).")]
        private bool _debugLog;

        /// <summary>The voices playing one note, shared by every cannon that shoots on it.</summary>
        private sealed class NoteSound
        {
            public ShotVoice Tone;
            public ShotVoice Boom;
            public int Count;
            public float ToneBaseVolume;
            public float BoomBaseVolume;
            public double EndDsp;
        }

        private readonly Dictionary<int, NoteSound> _sounds = new Dictionary<int, NoteSound>();
        private readonly List<ShotVoice> _activeVoices = new List<ShotVoice>();
        private readonly List<int> _finished = new List<int>();
        private ObjectPool<ShotVoice> _pool;

        /// <summary>AudioSources currently in use (for debugging: it should not keep growing).</summary>
        public int ActiveVoices => _activeVoices.Count;

        private void Awake()
        {
            if (_config == null || !HasPlayableSample(_config))
            {
                Debug.LogError("ShotNotePlayer needs a Shot Tone config with at least one sample that has a clip.", this);
                enabled = false;
                return;
            }

            _pool = new ObjectPool<ShotVoice>(CreateVoice, _config.MaxVoices);
        }

        public void Schedule(int noteIndex, RhythmNote note, double dspTime)
        {
            if (!enabled) return;

            if (_sounds.TryGetValue(noteIndex, out NoteSound shared))
            {
                // Another cannon on the same note: no new sound, just louder.
                shared.Count++;
                ApplyVolume(shared);
                if (_debugLog) Debug.Log($"[ShotNotePlayer] note #{noteIndex}: {shared.Count} cannons share one voice", this);
                return;
            }

            float velocity = note.Velocity > 0f ? note.Velocity : 1f; // 0 = unknown
            int target = TargetMidi(note.Pitch);
            ToneSample sample = ClosestSample(target);
            float pitch = Mathf.Pow(2f, (target - sample.RootMidi) / 12f);

            var sound = new NoteSound
            {
                Count = 1,
                ToneBaseVolume = _config.ToneVolume * velocity,
                BoomBaseVolume = _config.BoomVolume * velocity
            };
            sound.Tone = Play(sample.Clip, pitch, dspTime);
            if (_config.BoomClip != null) sound.Boom = Play(_config.BoomClip, 1f, dspTime);
            sound.EndDsp = System.Math.Max(sound.Tone.EndDsp, sound.Boom != null ? sound.Boom.EndDsp : 0.0);
            ApplyVolume(sound);
            _sounds[noteIndex] = sound;

            if (_debugLog)
                Debug.Log($"[ShotNotePlayer] note #{noteIndex}: MIDI {target} (sample root {sample.RootMidi}, pitch ×{pitch:0.###}) at dsp {dspTime:0.000}", this);
        }

        private void Update()
        {
            if (_sounds.Count == 0) return;

            double now = AudioSettings.dspTime;
            _finished.Clear();
            foreach (KeyValuePair<int, NoteSound> pair in _sounds)
                if (pair.Value.EndDsp <= now) _finished.Add(pair.Key);

            foreach (int noteIndex in _finished)
            {
                NoteSound sound = _sounds[noteIndex];
                if (sound.Tone != null) ReturnVoice(sound.Tone);
                if (sound.Boom != null) ReturnVoice(sound.Boom);
                _sounds.Remove(noteIndex);
            }
        }

        // ---------- Pitch ----------

        /// <summary>Note pitch (or the default for unpitched notes) + transpose, folded by octaves into [MinMidi, MaxMidi].</summary>
        private int TargetMidi(int pitch)
        {
            int midi = (pitch > 0 ? pitch : _config.UnpitchedMidi) + _config.TransposeSemitones;
            int min = _config.MinMidi, max = Mathf.Max(_config.MinMidi, _config.MaxMidi);

            if (max - min < 11) return Mathf.Clamp(midi, min, max); // range narrower than an octave: just clamp
            while (midi < min) midi += 12;
            while (midi > max) midi -= 12;
            return midi;
        }

        private ToneSample ClosestSample(int midi)
        {
            ToneSample best = default;
            int bestDistance = int.MaxValue;
            foreach (ToneSample sample in _config.Samples)
            {
                if (sample.Clip == null) continue;
                int distance = Mathf.Abs(sample.RootMidi - midi);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = sample;
                }
            }
            return best;
        }

        private static bool HasPlayableSample(ShotToneConfig config)
        {
            foreach (ToneSample sample in config.Samples)
                if (sample.Clip != null) return true;
            return false;
        }

        // ---------- Voices ----------

        private ShotVoice Play(AudioClip clip, float pitch, double dspTime)
        {
            ShotVoice voice = RentVoice();
            AudioSource source = voice.Source;
            source.clip = clip;
            source.pitch = pitch;
            source.PlayScheduled(dspTime);
            voice.EndDsp = dspTime + clip.length / Mathf.Max(0.01f, pitch);
            return voice;
        }

        private ShotVoice RentVoice()
        {
            // All voices busy: take the one that finishes first.
            if (_activeVoices.Count >= _config.MaxVoices)
            {
                ShotVoice earliest = _activeVoices[0];
                foreach (ShotVoice voice in _activeVoices)
                    if (voice.EndDsp < earliest.EndDsp) earliest = voice;
                StealVoice(earliest);
            }

            ShotVoice rented = _pool.Rent();
            _activeVoices.Add(rented);
            return rented;
        }

        private void StealVoice(ShotVoice voice)
        {
            foreach (NoteSound sound in _sounds.Values)
            {
                if (sound.Tone == voice) sound.Tone = null;
                if (sound.Boom == voice) sound.Boom = null;
            }
            ReturnVoice(voice);
        }

        private void ReturnVoice(ShotVoice voice)
        {
            if (_activeVoices.Remove(voice)) _pool.Return(voice);
        }

        private void ApplyVolume(NoteSound sound)
        {
            float stack = 1f + _config.StackGain * Mathf.Log(sound.Count, 2f);
            if (sound.Tone != null) sound.Tone.Source.volume = Mathf.Min(1f, sound.ToneBaseVolume * stack);
            if (sound.Boom != null) sound.Boom.Source.volume = Mathf.Min(1f, sound.BoomBaseVolume * stack);
        }

        private ShotVoice CreateVoice()
        {
            var go = new GameObject("Voice");
            go.transform.SetParent(transform, false);
            return go.AddComponent<ShotVoice>(); // AudioSource is added by RequireComponent
        }
    }
}
