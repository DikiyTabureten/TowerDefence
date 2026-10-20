using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Sets up sound in one click: import settings of the clips, CannonSound / BellSound on the tower prefabs,
    /// a "Music" AudioSource and a "Sound: On/Off" button in the Game scene, and the Bootstrap references.
    /// Safe to run again: existing components and objects are reused.
    /// </summary>
    public static class AudioSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Audio";

        private const string CannonClipPath = "Assets/Audio/SFX/cannon_shot.mp3";
        private const string BellClipPath = "Assets/Audio/SFX/bell_toll.mp3";
        private const string MusicClipPath = "Assets/Audio/Music/The_Old_Tower_Inn.mp3";
        private const string CannonPrefabPath = "Assets/Prefabs/ShooterTower.prefab";
        private const string BellPrefabPath = "Assets/Prefabs/SlowTower.prefab";
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        private const float MusicVolume = 0.35f;

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ConfigureClip(CannonClipPath, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip(BellClipPath, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip(MusicClipPath, AudioClipLoadType.Streaming);

            AddTowerSound<CannonSound>(CannonPrefabPath, "_shot", CannonClipPath);
            AddTowerSound<BellSound>(BellPrefabPath, "_toll", BellClipPath);

            SetupGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[AudioSetup] Done: tower sounds, background music source, sound toggle button.");
        }

        private static void ConfigureClip(string path, AudioClipLoadType loadType)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                Debug.LogError($"[AudioSetup] Clip not found: {path}");
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = loadType == AudioClipLoadType.Streaming;
            importer.SaveAndReimport();
        }

        private static void AddTowerSound<T>(string prefabPath, string clipField, string clipPath) where T : MonoBehaviour
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                AudioSource source = root.GetComponent<AudioSource>();
                if (source == null) source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f; // 2D: the whole map is on screen, distance shouldn't change volume

                T sound = root.GetComponent<T>();
                if (sound == null) sound = root.AddComponent<T>();
                var so = new SerializedObject(sound);
                so.FindProperty(clipField).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);

            // Background music: starts silent, BackgroundMusic plays it after the first orchestra.
            GameObject musicGo = GameObject.Find("Music");
            if (musicGo == null) musicGo = new GameObject("Music", typeof(AudioSource));
            var music = musicGo.GetComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClipPath);
            music.playOnAwake = false;
            music.loop = true;
            music.volume = MusicVolume;
            music.spatialBlend = 0f;

            SoundToggleView toggle = Object.FindFirstObjectByType<SoundToggleView>();
            if (toggle == null) toggle = CreateSoundToggle(Object.FindFirstObjectByType<HudView>().transform);

            var bootstrap = Object.FindFirstObjectByType<Bootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_musicSource").objectReferenceValue = music;
            so.FindProperty("_soundToggleView").objectReferenceValue = toggle;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>A "Sound: On" button in the top-right corner of the HUD canvas.</summary>
        private static SoundToggleView CreateSoundToggle(Transform canvas)
        {
            var go = new GameObject("SoundToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(200f, 56f);

            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.75f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "Sound: On";
            label.fontSize = 28;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var view = go.AddComponent<SoundToggleView>();
            var so = new SerializedObject(view);
            so.FindProperty("_button").objectReferenceValue = go.GetComponent<Button>();
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
