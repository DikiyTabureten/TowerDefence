using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>
    /// One click: creates Assets/Configs/Rhythm/RhythmTrack_Demo (pattern x.x.x.xxx.x.x.x., 4 steps per beat, 120 BPM,
    /// baked), puts a BeatConductor on the Game scene's Music object and assigns it in Bootstrap.
    /// Safe to run again: existing assets and components are reused.
    /// </summary>
    public static class RhythmSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Rhythm";
        private const string Folder = "Assets/Configs/Rhythm";
        private const string TrackPath = Folder + "/RhythmTrack_Demo.asset";
        private const string MusicClipPath = "Assets/Audio/Music/The_Old_Tower_Inn.mp3";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(Folder);
            RhythmTrackConfig track = CreateDemoTrack();

            var scene = EditorSceneManager.OpenScene(GameScenePath);
            track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(TrackPath); // reload after the scene switch

            GameObject music = GameObject.Find("Music");
            if (music == null) music = new GameObject("Music", typeof(AudioSource));
            var source = music.GetComponent<AudioSource>();
            source.playOnAwake = false;

            var conductor = music.GetComponent<BeatConductor>();
            if (conductor == null) conductor = music.AddComponent<BeatConductor>();
            var conductorSo = new SerializedObject(conductor);
            conductorSo.FindProperty("_track").objectReferenceValue = track;
            conductorSo.ApplyModifiedPropertiesWithoutUndo();

            var bootstrap = Object.FindFirstObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                var bootstrapSo = new SerializedObject(bootstrap);
                bootstrapSo.FindProperty("_beatConductor").objectReferenceValue = conductor;
                bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[RhythmSetup] No Bootstrap in the Game scene: assign Beat Conductor by hand.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = track;
            Debug.Log($"[RhythmSetup] Done: {TrackPath} ({track.Notes.Count} notes, {track.NotesPerSecond:0.##} notes/s), BeatConductor on 'Music'.", track);
        }

        private static RhythmTrackConfig CreateDemoTrack()
        {
            var track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(TrackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<RhythmTrackConfig>();
                AssetDatabase.CreateAsset(track, TrackPath);
            }

            var so = new SerializedObject(track);
            so.FindProperty("_pattern").stringValue = "x.x.x.xxx.x.x.x.";
            so.FindProperty("_stepsPerBeat").intValue = 4;
            so.FindProperty("_bpm").floatValue = 120f;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClipPath);
            if (clip != null) so.FindProperty("_clip").objectReferenceValue = clip;
            else Debug.LogWarning($"[RhythmSetup] Music not found at {MusicClipPath}: assign a Clip to the track by hand.");
            so.ApplyModifiedPropertiesWithoutUndo();

            RhythmTrackConfigEditor.BakeFromPattern(track);
            AssetDatabase.SaveAssets();
            return track;
        }
    }
}
