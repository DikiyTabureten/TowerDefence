using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Shop redesign, variant B "separate tiles": every tower card is its own dark HUD-style panel,
    /// the shop itself has no background. Rebuilds the card prefab (existing Icon and Price objects are
    /// moved, not recreated) and restyles the shop panel in the Game scene. Safe to run again.
    /// </summary>
    public static class ShopStyleSetup
    {
        private const string MenuPath = "Tower Defense/Setup/Apply Shop Style B";
        private const string CardPrefabPath = "Assets/Prefabs/Tower card.prefab";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string CoinIconPath = "Assets/Sprites/UI/Hud/icon_gold_pouch.png";
        private const string RoundedSprite = "UI/Skin/UISprite.psd";

        // HUD palette
        private static readonly Color Background = new Color32(0x20, 0x25, 0x2A, 0xFF);
        private static readonly Color Border = new Color32(0x19, 0x1E, 0x21, 0xFF);
        private static readonly Color SeparatorColor = new Color32(0x6D, 0x6F, 0x70, 0x99); // alpha 0.6
        private static readonly Color Gold = new Color32(0xFF, 0xC8, 0x3D, 0xFF);

        // 152 = border 3 + padding 10 + icon 84 + 6 + separator 2 + 6 + price row 30 + padding 8 + border 3. No name line.
        private static readonly Vector2 CardSize = new Vector2(112f, 152f);
        private const float BorderWidth = 3f;
        private const float IconSize = 84f;
        private const float CoinSize = 28f;
        private const float PriceFontSize = 26f;
        private const float ShopBottomOffset = 16f;
        private const float CardSpacing = 12f;

        [MenuItem(MenuPath)]
        private static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(GameScenePath);
            TMP_FontAsset font = HudFont();
            StyleShopPanel();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            RebuildCard(font);
            Debug.Log("[ShopStyleSetup] Shop style B applied: card prefab rebuilt, shop panel restyled.");
        }

        private static TMP_FontAsset HudFont()
        {
            var hud = Object.FindFirstObjectByType<HudView>();
            var gold = hud != null ? (TMP_Text)new SerializedObject(hud).FindProperty("_goldText").objectReferenceValue : null;
            return gold != null ? gold.font : TMP_Settings.defaultFontAsset;
        }

        // ---------- Shop panel ----------

        private static void StyleShopPanel()
        {
            var shop = Object.FindFirstObjectByType<TowerShopView>();
            var so = new SerializedObject(shop);
            var panel = (RectTransform)so.FindProperty("_shopPanel").objectReferenceValue;
            var container = (RectTransform)so.FindProperty("_cardsContainer").objectReferenceValue;

            // No shared background, but keep the object: IsPointerOverShop uses its rectangle.
            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0f, 0f, 0f, 0f);

            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, ShopBottomOffset);

            // The panel's size follows the cards: its layout group reports the container's preferred size.
            var panelLayout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
            panelLayout.childControlWidth = panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = panelLayout.childForceExpandHeight = false;
            panelLayout.childAlignment = TextAnchor.LowerCenter;
            var panelFitter = GetOrAdd<ContentSizeFitter>(panel.gameObject);
            panelFitter.horizontalFit = panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var row = GetOrAdd<HorizontalLayoutGroup>(container.gameObject);
            row.spacing = CardSpacing;
            row.childAlignment = TextAnchor.LowerCenter;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.padding = new RectOffset(0, 0, 0, 0);
            // No ContentSizeFitter here: the panel's layout group already sizes the container,
            // and two components driving the same RectTransform fight each other.
            var oldFitter = container.GetComponent<ContentSizeFitter>();
            if (oldFitter != null) Object.DestroyImmediate(oldFitter);
        }

        // ---------- Card prefab ----------

        private static void RebuildCard(TMP_FontAsset font)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSprite);
            Sprite coin = AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);

            GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                var view = root.GetComponent<TowerCardView>();
                var viewSo = new SerializedObject(view);
                var icon = (Image)viewSo.FindProperty("_icon").objectReferenceValue;
                var price = (TMP_Text)viewSo.FindProperty("_priceText").objectReferenceValue;

                // Root: transparent, but still the raycast target for drag & hover.
                var rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = CardSize;
                var rootImage = GetOrAdd<Image>(root);
                rootImage.sprite = null;
                rootImage.color = new Color(1f, 1f, 1f, 0f);
                rootImage.raycastTarget = true;
                GetOrAdd<CanvasGroup>(root).alpha = 1f;

                // Body = border; Background = card face with the vertical layout.
                RectTransform body = Child(root.transform, "Body");
                Stretch(body, 0f);
                Image border = GetOrAdd<Image>(body.gameObject);
                SetImage(border, rounded, Border);

                RectTransform background = Child(body, "Background");
                Stretch(background, BorderWidth);
                SetImage(GetOrAdd<Image>(background.gameObject), rounded, Background);
                var column = GetOrAdd<VerticalLayoutGroup>(background.gameObject);
                column.padding = new RectOffset(0, 0, 10, 8);
                column.spacing = 6f;
                column.childAlignment = TextAnchor.UpperCenter;
                column.childControlWidth = column.childControlHeight = false;
                column.childForceExpandWidth = column.childForceExpandHeight = false;

                // Icon (existing object, moved)
                icon.transform.SetParent(background, false);
                icon.transform.SetSiblingIndex(0);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(IconSize, IconSize);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                // No name line on the card: the icon is enough. Remove it if an earlier run created it.
                Transform oldName = background.Find("Name");
                if (oldName != null) Object.DestroyImmediate(oldName.gameObject);

                // Separator
                RectTransform separator = Child(background, "Separator");
                separator.SetSiblingIndex(1);
                separator.sizeDelta = new Vector2(80f, 2f);
                SetImage(GetOrAdd<Image>(separator.gameObject), null, SeparatorColor);

                // Price row: coin + price (existing Price object, moved)
                RectTransform priceRow = Child(background, "PriceRow");
                priceRow.SetSiblingIndex(2);
                priceRow.sizeDelta = new Vector2(100f, 30f);
                var rowLayout = GetOrAdd<HorizontalLayoutGroup>(priceRow.gameObject);
                rowLayout.spacing = 6f;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true; // use the price text's real width
                rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

                RectTransform coinRect = Child(priceRow, "CoinIcon");
                coinRect.SetSiblingIndex(0);
                Image coinImage = GetOrAdd<Image>(coinRect.gameObject);
                SetImage(coinImage, coin, Color.white);
                coinImage.type = Image.Type.Simple;
                coinImage.preserveAspect = true;
                var coinLayout = GetOrAdd<LayoutElement>(coinRect.gameObject);
                coinLayout.preferredWidth = coinLayout.preferredHeight = CoinSize;

                price.transform.SetParent(priceRow, false);
                price.transform.SetSiblingIndex(1);
                price.font = font;
                price.fontSize = PriceFontSize;
                price.fontStyle = FontStyles.Bold;
                price.color = Gold;
                price.alignment = TextAlignmentOptions.MidlineLeft;
                price.textWrappingMode = TextWrappingModes.NoWrap;
                price.raycastTarget = false;

                viewSo.FindProperty("_nameText").objectReferenceValue = null; // optional in TowerCardView
                viewSo.FindProperty("_body").objectReferenceValue = body;
                viewSo.FindProperty("_border").objectReferenceValue = border;
                viewSo.FindProperty("_coinIcon").objectReferenceValue = coinImage;
                viewSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Helpers ----------

        private static RectTransform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform)).transform;
                child.SetParent(parent, false);
            }
            return (RectTransform)child;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetImage(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
