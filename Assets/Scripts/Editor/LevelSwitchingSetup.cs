using System.IO;
using TMPro;
using TowerDefense.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Sets up level switching in one click:
    /// 1) LevelCatalog with the three levels in order;
    /// 2) the game scene is named "Game", its Bootstrap gets the catalog and the default level,
    ///    its result panel gets "Next level" / "Restart" / "Menu" buttons wired to HudView;
    /// 3) a LevelButton prefab and the MainMenu scene (View + Presenter);
    /// 4) Build Settings: MainMenu (0), Game (1).
    /// Safe to run again: existing assets and objects are reused.
    /// </summary>
    public static class LevelSwitchingSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Level Switching";

        private const string LevelsFolder = "Assets/Configs/Levels";
        private const string CatalogPath = LevelsFolder + "/LevelCatalog.asset";
        private const string OldGameScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GameScenePath = "Assets/Scenes/" + SceneNavigator.GameScene + ".unity";
        private const string MenuScenePath = "Assets/Scenes/" + SceneNavigator.MainMenuScene + ".unity";
        private const string LevelButtonPrefabPath = "Assets/Prefabs/UI/LevelButton.prefab";

        private static readonly string[] LevelPaths =
        {
            LevelsFolder + "/Level_01_VillageRoad.asset",
            LevelsFolder + "/Level_02_RiverCrossing.asset",
            LevelsFolder + "/Level_03_CastleSiege.asset",
        };

        private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.12f, 0.85f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.26f, 0.3f, 1f);
        private static readonly Color MenuBackground = new Color(0.16f, 0.45f, 0.28f, 1f);

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateCatalog();
            AssetDatabase.SaveAssets();
            SetupGameScene();
            CreateLevelButtonPrefab();
            CreateMenuScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            EditorSceneManager.OpenScene(GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelSwitchingSetup] Done: LevelCatalog, MainMenu scene, result buttons, Build Settings (MainMenu, Game).");
        }

        // ---------- Catalog ----------

        private static LevelCatalog CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty levels = so.FindProperty("_levels");
            levels.arraySize = LevelPaths.Length;
            for (int i = 0; i < LevelPaths.Length; i++)
                levels.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        // ---------- Game scene ----------

        private static void SetupGameScene()
        {
            // The scene keeps its GUID when renamed, so nothing that references it breaks.
            if (!File.Exists(GameScenePath) && File.Exists(OldGameScenePath))
            {
                string error = AssetDatabase.RenameAsset(OldGameScenePath, SceneNavigator.GameScene);
                if (!string.IsNullOrEmpty(error)) Debug.LogError($"[LevelSwitchingSetup] Could not rename the scene: {error}");
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath);
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath); // load after the scene switch

            var bootstrap = Object.FindFirstObjectByType<Bootstrap>();
            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("_catalog").objectReferenceValue = catalog;
            if (bootstrapSo.FindProperty("_defaultLevel").objectReferenceValue == null)
                bootstrapSo.FindProperty("_defaultLevel").objectReferenceValue = catalog.Get(0);
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            var hud = Object.FindFirstObjectByType<HudView>();
            var hudSo = new SerializedObject(hud);
            var panel = (GameObject)hudSo.FindProperty("_resultPanel").objectReferenceValue;
            var resultText = (TMP_Text)hudSo.FindProperty("_resultText").objectReferenceValue;

            StyleResultPanel(panel, resultText);
            Transform row = GetOrCreateButtonRow(panel.transform);
            hudSo.FindProperty("_nextLevelButton").objectReferenceValue = GetOrCreateButton(row, "NextLevelButton", "Next level");
            hudSo.FindProperty("_restartButton").objectReferenceValue = GetOrCreateButton(row, "RestartButton", "Restart");
            hudSo.FindProperty("_menuButton").objectReferenceValue = GetOrCreateButton(row, "MenuButton", "Menu");
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void StyleResultPanel(GameObject panel, TMP_Text resultText)
        {
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = new Vector2(640f, 280f);
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = PanelColor;

            var textRect = resultText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -30f);
            textRect.sizeDelta = new Vector2(-40f, 100f);
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontSize = 56;
            resultText.fontStyle = FontStyles.Bold;
        }

        private static Transform GetOrCreateButtonRow(Transform panel)
        {
            Transform row = panel.Find("Buttons");
            if (row == null)
            {
                var go = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                go.transform.SetParent(panel, false);
                row = go.transform;
            }

            var rect = (RectTransform)row;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(-40f, 70f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<Button>();

            Button button = CreateButton(parent, name, new Vector2(180f, 64f));
            TextMeshProUGUI text = CreateText(button.transform, "Label", label, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, Vector2.zero);
            return button;
        }

        // ---------- Main menu ----------

        private static LevelButtonView CreateLevelButtonPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LevelButtonView>(LevelButtonPrefabPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(LevelButtonPrefabPath));
            var root = new GameObject("LevelButton", typeof(RectTransform));
            try
            {
                var image = root.AddComponent<Image>();
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
                image.color = ButtonColor;
                var rootButton = root.AddComponent<Button>();
                var layoutElement = root.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 760f;
                layoutElement.preferredHeight = 120f;

                TextMeshProUGUI title = CreateText(root.transform, "Title", "1. Level", 40, FontStyles.Bold, TextAlignmentOptions.TopLeft);
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-48f, 50f));
                TextMeshProUGUI description = CreateText(root.transform, "Description", "Description", 24, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                description.color = new Color(1f, 1f, 1f, 0.8f);
                SetRect(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(-48f, -64f));

                var view = root.AddComponent<LevelButtonView>();
                var so = new SerializedObject(view);
                so.FindProperty("_button").objectReferenceValue = rootButton;
                so.FindProperty("_title").objectReferenceValue = title;
                so.FindProperty("_description").objectReferenceValue = description;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, LevelButtonPrefabPath).GetComponent<LevelButtonView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateMenuScene()
        {
            var scene = File.Exists(MenuScenePath)
                ? EditorSceneManager.OpenScene(MenuScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets only after switching scenes: opening a scene unloads unused assets,
            // and references loaded before that would be saved as empty.
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            var buttonPrefab = AssetDatabase.LoadAssetAtPath<LevelButtonView>(LevelButtonPrefabPath);

            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var cameraGo = new GameObject("Main Camera", typeof(Camera));
                cameraGo.tag = "MainCamera";
                var camera = cameraGo.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = MenuBackground;
                cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                TextMeshProUGUI title = CreateText(canvas.transform, "Title", "Tower Defense", 96, FontStyles.Bold, TextAlignmentOptions.Center);
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(0f, 140f));

                var list = new GameObject("LevelList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                list.transform.SetParent(canvas.transform, false);
                SetRect((RectTransform)list.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(760f, 0f));
                var layout = list.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 24f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var view = canvas.gameObject.AddComponent<LevelSelectView>();
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("_buttonsContainer").objectReferenceValue = list.transform;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var levelSelectView = canvas.GetComponent<LevelSelectView>();
            var levelSelectSo = new SerializedObject(levelSelectView);
            levelSelectSo.FindProperty("_buttonPrefab").objectReferenceValue = buttonPrefab;
            levelSelectSo.ApplyModifiedPropertiesWithoutUndo();

            var menuBootstrap = Object.FindFirstObjectByType<MainMenuBootstrap>();
            if (menuBootstrap == null)
                menuBootstrap = new GameObject("MainMenuBootstrap").AddComponent<MainMenuBootstrap>();
            var so = new SerializedObject(menuBootstrap);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.FindProperty("_view").objectReferenceValue = levelSelectView;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // ---------- UI helpers ----------

        private static Button CreateButton(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = ButtonColor;
            return go.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
            FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Stretch(RectTransform rect, Vector2 padding) =>
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, -padding * 2f);

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
