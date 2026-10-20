using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Builds the HUD bar in the Game scene: one dark rounded panel in the top-left corner with
    /// three "icon + number" blocks (gold, lives, wave) separated by thin lines. The existing
    /// GoldText / LivesText / WaveText objects are moved into it, so HudView keeps its references.
    /// Safe to run again: the bar is reused and restyled.
    /// </summary>
    public static class HudSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup HUD";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string IconFolder = "Assets/Sprites/UI/Hud";

        private const float IconSize = 60f;
        private const float FontSize = 42f;

        private static readonly Color PanelColor = new Color32(0x1E, 0x21, 0x26, 0xEB);
        private static readonly Color BorderColor = new Color32(0x4A, 0x4F, 0x57, 0xFF);
        private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color GoldColor = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
        private static readonly Color LivesColor = Color.white;
        private static readonly Color WaveColor = new Color32(0xCF, 0xBD, 0xFF, 0xFF);

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Sprite gold = ImportIcon("icon_gold_pouch");
            Sprite lives = ImportIcon("icon_lives_shield");
            Sprite wave = ImportIcon("icon_wave_swords");

            var scene = EditorSceneManager.OpenScene(GameScenePath);
            var hud = Object.FindFirstObjectByType<HudView>();
            var hudSo = new SerializedObject(hud);
            var goldText = (TMP_Text)hudSo.FindProperty("_goldText").objectReferenceValue;
            var livesText = (TMP_Text)hudSo.FindProperty("_livesText").objectReferenceValue;
            var waveText = (TMP_Text)hudSo.FindProperty("_waveText").objectReferenceValue;

            // Icons are loaded again after the scene switch: opening a scene unloads unused assets.
            gold = LoadIcon("icon_gold_pouch");
            lives = LoadIcon("icon_lives_shield");
            wave = LoadIcon("icon_wave_swords");

            RectTransform bar = GetOrCreateBar(hud.transform);
            Item(bar, "Gold", gold, goldText, GoldColor, "150", 0);
            Separator(bar, "Separator1", 1);
            Item(bar, "Lives", lives, livesText, LivesColor, "10", 2);
            Separator(bar, "Separator2", 3);
            Item(bar, "Wave", wave, waveText, WaveColor, "0/5", 4);

            var soundToggle = Object.FindFirstObjectByType<SoundToggleView>();
            if (soundToggle != null) StyleLikeBar(soundToggle.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HudSetup] HUD bar built: gold, lives and wave in one panel.");
        }

        private static Sprite ImportIcon(string name)
        {
            string path = $"{IconFolder}/{name}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; // crisp UI icons
                importer.SaveAndReimport();
            }
            else Debug.LogError($"[HudSetup] Icon not found: {path}");
            return LoadIcon(name);
        }

        private static Sprite LoadIcon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");

        private static RectTransform GetOrCreateBar(Transform canvas)
        {
            Transform existing = canvas.Find("HudBar");
            GameObject bar = existing != null
                ? existing.gameObject
                : new GameObject("HudBar", typeof(RectTransform), typeof(Image), typeof(Outline),
                    typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            bar.transform.SetParent(canvas, false);
            bar.transform.SetAsFirstSibling(); // behind the result panel and other UI

            var rect = (RectTransform)bar.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);

            var image = bar.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = PanelColor;
            image.raycastTarget = false;

            var outline = bar.GetComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 22, 10, 10);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = bar.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        /// <summary>One block: icon + number. The HudView text object is moved here and restyled.</summary>
        private static void Item(RectTransform bar, string name, Sprite icon, TMP_Text text, Color color,
            string placeholder, int siblingIndex)
        {
            Transform existing = bar.Find(name);
            GameObject item = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            item.transform.SetParent(bar, false);
            item.transform.SetSiblingIndex(siblingIndex);

            var layout = item.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Transform iconTransform = item.transform.Find("Icon");
            GameObject iconGo = iconTransform != null
                ? iconTransform.gameObject
                : new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGo.transform.SetParent(item.transform, false);
            iconGo.transform.SetSiblingIndex(0);
            var image = iconGo.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var iconLayout = iconGo.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = IconSize;

            if (text == null) return;
            text.transform.SetParent(item.transform, false);
            text.transform.SetSiblingIndex(1);
            text.text = placeholder;
            text.fontSize = FontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            var textLayout = text.GetComponent<LayoutElement>();
            if (textLayout == null) textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.minWidth = 56f; // numbers growing from 9 to 10 don't make the bar jump much
        }

        /// <summary>Other HUD buttons (sound toggle) get the same panel colour and border as the bar.</summary>
        private static void StyleLikeBar(GameObject button)
        {
            var image = button.GetComponent<Image>();
            if (image != null) image.color = PanelColor;
            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.AddComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, IconSize + 20f); // same height as the bar
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.fontStyle = FontStyles.Bold;
        }

        private static void Separator(RectTransform bar, string name, int siblingIndex)
        {
            Transform existing = bar.Find(name);
            GameObject separator = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            separator.transform.SetParent(bar, false);
            separator.transform.SetSiblingIndex(siblingIndex);
            separator.GetComponent<Image>().color = SeparatorColor;
            separator.GetComponent<Image>().raycastTarget = false;
            var layout = separator.GetComponent<LayoutElement>();
            layout.preferredWidth = 2f;
            layout.preferredHeight = 40f;
        }
    }
}
