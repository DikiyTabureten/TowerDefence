using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>A detected note start: time in seconds and strength 0..1.</summary>
    internal struct Onset
    {
        public float Time;
        public float Strength;
    }

    /// <summary>
    /// Onset detection for songs without notes. Mono mix → frames of 1024 samples (hop 512) with a Hann window →
    /// own radix-2 FFT → spectral flux (sum of positive magnitude increases) → adaptive threshold
    /// (local mean over ~0.5 s × sensitivity + small offset) → peaks, at least MinGap apart.
    /// BPM is estimated by autocorrelation of the onset envelope in the 60–180 range. No external libraries.
    /// </summary>
    internal static class AudioOnsetAnalyzer
    {
        public const int FrameSize = 1024;
        public const int HopSize = 512;
        private const float ThresholdOffset = 0.02f;   // on flux normalised to 0..1
        private const float MeanWindowSeconds = 0.5f;

        /// <summary>Spectral flux of a clip, normalised to 0..1. FrameSeconds = time between two flux values.</summary>
        public sealed class Flux
        {
            public float[] Values;
            public float FrameSeconds;
            public float ClipLength;
        }

        // ---------- Reading samples ----------

        /// <summary>
        /// Mono samples of the clip. If the clip's import does not allow GetData (Streaming / Compressed In Memory,
        /// or loading in the background), it is switched to Decompress On Load for the analysis and switched back afterwards.
        /// </summary>
        public static float[] ReadMono(AudioClip clip, out int sampleRate)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            AudioImporterSampleSettings original = importer != null ? importer.defaultSampleSettings : default;
            bool originalLoadInBackground = importer != null && importer.loadInBackground;
            bool switched = false;

            if (importer != null && (original.loadType != AudioClipLoadType.DecompressOnLoad || originalLoadInBackground))
            {
                AudioImporterSampleSettings temporary = original;
                temporary.loadType = AudioClipLoadType.DecompressOnLoad;
                temporary.preloadAudioData = true;
                importer.defaultSampleSettings = temporary;
                importer.loadInBackground = false;
                importer.SaveAndReimport();
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                switched = true;
            }

            try
            {
                if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

                // Loading can still be asynchronous: wait for it (up to ~15 s).
                var timer = System.Diagnostics.Stopwatch.StartNew();
                while (clip.loadState == AudioDataLoadState.Loading && timer.ElapsedMilliseconds < 15000)
                    System.Threading.Thread.Sleep(10);
                if (clip.loadState != AudioDataLoadState.Loaded)
                    throw new InvalidOperationException($"Could not load audio data of '{clip.name}' (state: {clip.loadState}).");

                int channels = Mathf.Max(1, clip.channels);
                var interleaved = new float[clip.samples * channels];
                if (!clip.GetData(interleaved, 0))
                    throw new InvalidOperationException($"Could not read samples of '{clip.name}'.");

                var mono = new float[clip.samples];
                for (int i = 0; i < mono.Length; i++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++) sum += interleaved[i * channels + c];
                    mono[i] = sum / channels;
                }

                sampleRate = clip.frequency;
                return mono;
            }
            finally
            {
                if (switched)
                {
                    importer.defaultSampleSettings = original;
                    importer.loadInBackground = originalLoadInBackground;
                    importer.SaveAndReimport();
                }
            }
        }

        // ---------- Spectral flux ----------

        public static Flux ComputeFlux(float[] mono, int sampleRate)
        {
            int frames = mono.Length < FrameSize ? 0 : (mono.Length - FrameSize) / HopSize + 1;
            var flux = new float[frames];
            var window = new float[FrameSize];
            for (int i = 0; i < FrameSize; i++)
                window[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (FrameSize - 1)); // Hann

            var re = new float[FrameSize];
            var im = new float[FrameSize];
            var previous = new float[FrameSize / 2];
            var magnitude = new float[FrameSize / 2];
            float max = 0f;

            for (int f = 0; f < frames; f++)
            {
                int offset = f * HopSize;
                for (int i = 0; i < FrameSize; i++)
                {
                    re[i] = mono[offset + i] * window[i];
                    im[i] = 0f;
                }

                Fft(re, im);

                float sum = 0f;
                for (int k = 0; k < magnitude.Length; k++)
                {
                    magnitude[k] = Mathf.Sqrt(re[k] * re[k] + im[k] * im[k]);
                    float rise = magnitude[k] - previous[k];
                    if (rise > 0f) sum += rise;
                }

                flux[f] = f == 0 ? 0f : sum; // the first frame has nothing to compare with
                Array.Copy(magnitude, previous, magnitude.Length);
                if (flux[f] > max) max = flux[f];
            }

            if (max > 0f)
                for (int f = 0; f < frames; f++) flux[f] /= max;

            return new Flux
            {
                Values = flux,
                FrameSeconds = (float)HopSize / sampleRate,
                ClipLength = (float)mono.Length / sampleRate
            };
        }

        /// <summary>In-place iterative radix-2 Cooley–Tukey FFT. Length must be a power of two.</summary>
        private static void Fft(float[] re, float[] im)
        {
            int n = re.Length;

            // Bit-reversal permutation
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (re[i], re[j]) = (re[j], re[i]);
                    (im[i], im[j]) = (im[j], im[i]);
                }
            }

            // Butterflies
            for (int length = 2; length <= n; length <<= 1)
            {
                double angle = -2.0 * Math.PI / length;
                float stepRe = (float)Math.Cos(angle);
                float stepIm = (float)Math.Sin(angle);
                int half = length >> 1;

                for (int start = 0; start < n; start += length)
                {
                    float wRe = 1f, wIm = 0f;
                    for (int k = 0; k < half; k++)
                    {
                        int a = start + k;
                        int b = a + half;
                        float tRe = re[b] * wRe - im[b] * wIm;
                        float tIm = re[b] * wIm + im[b] * wRe;
                        re[b] = re[a] - tRe;
                        im[b] = im[a] - tIm;
                        re[a] += tRe;
                        im[a] += tIm;

                        float nextRe = wRe * stepRe - wIm * stepIm;
                        wIm = wRe * stepIm + wIm * stepRe;
                        wRe = nextRe;
                    }
                }
            }
        }

        // ---------- Onsets ----------

        /// <param name="sensitivity">Threshold = local mean × this. Higher = stricter = fewer notes.</param>
        public static List<Onset> PickOnsets(Flux flux, float sensitivity, float minGapSeconds)
        {
            float[] values = flux.Values;
            float[] mean = LocalMean(values, Mathf.Max(1, Mathf.RoundToInt(MeanWindowSeconds / flux.FrameSeconds)));
            var onsets = new List<Onset>();

            for (int i = 1; i < values.Length - 1; i++)
            {
                float threshold = mean[i] * sensitivity + ThresholdOffset;
                bool isPeak = values[i] > threshold && values[i] >= values[i - 1] && values[i] > values[i + 1];
                if (!isPeak) continue;

                var onset = new Onset { Time = i * flux.FrameSeconds, Strength = Mathf.Clamp01(values[i]) };
                if (onsets.Count > 0 && onset.Time - onsets[onsets.Count - 1].Time < minGapSeconds)
                {
                    // Too close to the previous one: keep the stronger of the two.
                    if (onset.Strength > onsets[onsets.Count - 1].Strength) onsets[onsets.Count - 1] = onset;
                    continue;
                }
                onsets.Add(onset);
            }

            return onsets;
        }

        /// <summary>Tempo from the autocorrelation of the onset envelope, searched between minBpm and maxBpm.</summary>
        public static float EstimateBpm(Flux flux, float minBpm = 60f, float maxBpm = 180f)
        {
            float[] values = flux.Values;
            float[] mean = LocalMean(values, Mathf.Max(1, Mathf.RoundToInt(MeanWindowSeconds / flux.FrameSeconds)));

            // Envelope: only the part of the flux above its local mean (the "attacks").
            var envelope = new float[values.Length];
            for (int i = 0; i < values.Length; i++) envelope[i] = Mathf.Max(0f, values[i] - mean[i]);

            int minLag = Mathf.Max(1, Mathf.FloorToInt(60f / maxBpm / flux.FrameSeconds));
            int maxLag = Mathf.Min(envelope.Length - 2, Mathf.CeilToInt(60f / minBpm / flux.FrameSeconds));
            if (maxLag <= minLag) return 120f;

            var scores = new float[maxLag + 2];
            int bestLag = minLag;
            float bestScore = float.MinValue;
            for (int lag = minLag; lag <= maxLag + 1 && lag < envelope.Length; lag++)
            {
                double sum = 0.0;
                for (int i = 0; i + lag < envelope.Length; i++) sum += envelope[i] * envelope[i + lag];
                float score = (float)(sum / (envelope.Length - lag));

                // Gentle preference for tempos near 120 to avoid picking half or double time.
                float bpm = 60f / (lag * flux.FrameSeconds);
                float octaves = Mathf.Log(bpm / 120f, 2f);
                score *= Mathf.Exp(-0.5f * octaves * octaves);

                scores[lag] = score;
                if (lag <= maxLag && score > bestScore)
                {
                    bestScore = score;
                    bestLag = lag;
                }
            }

            // Parabolic interpolation around the best lag for a finer tempo.
            float refined = bestLag;
            if (bestLag > minLag && bestLag + 1 < scores.Length)
            {
                float left = scores[bestLag - 1], centre = scores[bestLag], right = scores[bestLag + 1];
                float denominator = left - 2f * centre + right;
                if (Mathf.Abs(denominator) > 1e-9f) refined += 0.5f * (left - right) / denominator;
            }

            return Mathf.Round(600f / (refined * flux.FrameSeconds)) / 10f; // 60 / seconds, one decimal
        }

        /// <summary>Onsets → notes in beats. Quantize snaps to the grid (in beats) and removes duplicates.</summary>
        public static List<RhythmNote> ToNotes(List<Onset> onsets, float bpm, float loopBeats, bool quantize, float gridBeats)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);
            var byBeat = new SortedDictionary<float, RhythmNote>();

            foreach (Onset onset in onsets)
            {
                float beat = onset.Time / secondsPerBeat;
                if (quantize && gridBeats > 0f) beat = Mathf.Round(beat / gridBeats) * gridBeats;
                if (beat < 0f || beat >= loopBeats) continue;

                // Duplicates after quantizing: keep the strongest.
                if (byBeat.TryGetValue(beat, out RhythmNote existing) && existing.Velocity >= onset.Strength) continue;
                byBeat[beat] = new RhythmNote(beat, 0, Mathf.Clamp01(onset.Strength));
            }

            return new List<RhythmNote>(byBeat.Values);
        }

        private static float[] LocalMean(float[] values, int window)
        {
            var prefix = new double[values.Length + 1];
            for (int i = 0; i < values.Length; i++) prefix[i + 1] = prefix[i] + values[i];

            var mean = new float[values.Length];
            int half = window / 2;
            for (int i = 0; i < values.Length; i++)
            {
                int from = Mathf.Max(0, i - half);
                int to = Mathf.Min(values.Length, i + half + 1);
                mean[i] = (float)((prefix[to] - prefix[from]) / (to - from));
            }
            return mean;
        }
    }
}
