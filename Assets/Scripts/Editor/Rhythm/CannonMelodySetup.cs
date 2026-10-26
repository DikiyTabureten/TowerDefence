using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>
    /// "The cannons play the melody" in one click:
    /// - import settings of the tonal layers (Decompress On Load, Preload, Force To Mono);
    /// - Assets/Configs/Audio/ShotTone_Default (tone sample + the existing cannon shot as the boom);
    /// - Assets/Configs/Rhythm/RhythmTrack_Korobeiniki imported from Korobeiniki_test.mid, without a music clip;
    /// - disables the old CannonSound on the cannon prefab (ShotNotePlayer plays the boom now, on time);
    /// - a ShotNotePlayer object in the Game scene, wired into Bootstrap; the BeatConductor gets the new track.
    /// Safe to run again.
    /// </summary>
    public static class CannonMelodySetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Cannon Melody";
        private const string TonesFolder = "Assets/Audio/CannonTones";
        private const string MidiPath = TonesFolder + "/Korobeiniki_test.mid";
        private const string BoomClipPath = "Assets/Audio/SFX/cannon_shot.mp3";
        private const string ToneConfigPath = "Assets/Configs/Audio/ShotTone_Default.asset";
        private const string TrackPath = "Assets/Configs/Rhythm/RhythmTrack_Korobeiniki.asset";
        private const string CannonPrefabPath = "Assets/Prefabs/ShooterTower.prefab";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string tonePath = ConfigureToneLayers();
            if (tonePath == null)
            {
                Debug.LogError($"[CannonMelodySetup] No ToneLayer_*.wav in {TonesFolder}.");
                return;
            }

            CreateToneConfig(tonePath);
            if (!CreateMelodyTrack()) return;
            DisableOldCannonSound();
            SetupScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[CannonMelodySetup] Done: ShotTone_Default, RhythmTrack_Korobeiniki, ShotNotePlayer in the Game scene.");
        }

        /// <summary>Import settings for every tonal layer; returns the path of the first one.</summary>
        private static string ConfigureToneLayers()
        {
            string first = null;
            foreach (string guid in AssetDatabase.FindAssets("ToneLayer_ t:AudioClip", new[] { TonesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();

                if (first == null || string.CompareOrdinal(path, first) < 0) first = path;
            }
            return first;
        }

        private static void CreateToneConfig(string tonePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToneConfigPath));
            var config = LoadOrCreate<ShotToneConfig>(ToneConfigPath);

            var so = new SerializedObject(config);
            SerializedProperty samples = so.FindProperty("_samples");
            samples.arraySize = 1;
            SerializedProperty sample = samples.GetArrayElementAtIndex(0);
            sample.FindPropertyRelative("Clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(tonePath);
            sample.FindPropertyRelative("RootMidi").intValue = RootFromName(Path.GetFileNameWithoutExtension(tonePath));
            so.FindProperty("_boomClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BoomClipPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>"ToneLayer_A_timpani_C4" → 60, "…_F#3" → 54. Falls back to 60 (C4).</summary>
        private static int RootFromName(string name)
        {
            Match match = Regex.Match(name, @"_([A-G])(#|b)?(-?\d)$");
            if (!match.Success) return 60;

            int[] semitones = { 9, 11, 0, 2, 4, 5, 7 }; // A B C D E F G
            int note = semitones[match.Groups[1].Value[0] - 'A'];
            if (match.Groups[2].Value == "#") note++;
            if (match.Groups[2].Value == "b") note--;
            int octave = int.Parse(match.Groups[3].Value);
            return (octave + 1) * 12 + note;
        }

        private static bool CreateMelodyTrack()
        {
            if (!File.Exists(MidiPath))
            {
                Debug.LogError($"[CannonMelodySetup] MIDI file not found: {MidiPath}");
                return false;
            }

            var track = LoadOrCreate<RhythmTrackConfig>(TrackPath);
            var so = new SerializedObject(track);
            so.FindProperty("_clip").objectReferenceValue = null; // only the cannons play
            so.ApplyModifiedPropertiesWithoutUndo();

            MidiImportWindow.ImportFile(track, Path.GetFullPath(MidiPath));
            return true;
        }

        private static void DisableOldCannonSound()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CannonPrefabPath);
            try
            {
                var oldSound = root.GetComponent<CannonSound>();
                if (oldSound != null && oldSound.enabled)
                {
                    oldSound.enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(root, CannonPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);
            // Load assets after the scene switch: opening a scene unloads unused assets.
            var toneConfig = AssetDatabase.LoadAssetAtPath<ShotToneConfig>(ToneConfigPath);
            var track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(TrackPath);

            var player = Object.FindFirstObjectByType<ShotNotePlayer>();
            if (player == null) player = new GameObject("ShotNotePlayer").AddComponent<ShotNotePlayer>();
            var playerSo = new SerializedObject(player);
            playerSo.FindProperty("_config").objectReferenceValue = toneConfig;
            playerSo.ApplyModifiedPropertiesWithoutUndo();

            var conductor = Object.FindFirstObjectByType<BeatConductor>();
            if (conductor != null)
            {
                var conductorSo = new SerializedObject(conductor);
                conductorSo.FindProperty("_track").objectReferenceValue = track;
                conductorSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[CannonMelodySetup] No BeatConductor in the Game scene: run Setup Rhythm first.");

            var bootstrap = Object.FindFirstObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                var bootstrapSo = new SerializedObject(bootstrap);
                bootstrapSo.FindProperty("_shotNotePlayer").objectReferenceValue = player;
                bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
