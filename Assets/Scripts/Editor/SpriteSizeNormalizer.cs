using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Applies the project's scale standard: the size of a sprite in the world is set by
    /// Pixels Per Unit of its texture, not by the Scale of a prefab.
    /// One map cell = 1 unit. A rule either fixes PPU or gives a target size in units;
    /// for a target size PPU = (longest side of the visible, non-transparent part) / target.
    /// </summary>
    public static class SpriteSizeNormalizer
    {
        private const string MenuPath = "Tools/Tower Defense/Normalize Sprite Sizes";
        private const string ReportMenuPath = "Tools/Tower Defense/Normalize Sprite Sizes (Report Only)";
        private const byte AlphaThreshold = 10;
        private const int MaxDetailedLinesPerRule = 10; // the rest is summarised, e.g. 299 map tiles

        private readonly struct Rule
        {
            public readonly string PathPrefix;
            public readonly float TargetUnits; // > 0: compute PPU from the visible size
            public readonly float FixedPpu;    // > 0: use this PPU as is

            private Rule(string pathPrefix, float targetUnits, float fixedPpu)
            {
                PathPrefix = pathPrefix;
                TargetUnits = targetUnits;
                FixedPpu = fixedPpu;
            }

            public static Rule Size(string pathPrefix, float units) => new Rule(pathPrefix, units, 0f);
            public static Rule Ppu(string pathPrefix, float ppu) => new Rule(pathPrefix, 0f, ppu);
        }

        // Visible size of a tower base (cannon_base: 234 px at PPU 256). Enemy sizes are relative to it.
        private const float TowerSize = 234f / 256f;

        // Scale standard. The longest matching prefix wins.
        private static readonly Rule[] Rules =
        {
            // Map: 64x64 Kenney tiles, one tile = one cell.
            Rule.Ppu("Assets/Sprites/Map", 64f),
            Rule.Ppu("Assets/Sprites/Map/Retina", 128f), // the same tiles at 128x128, used by MapTheme

            // Towers: 256 px canvas, the base (~230 px visible) fills ~0.9 of a cell.
            // Every part of a tower uses the same PPU as its base, so local offsets stay valid.
            Rule.Ppu("Assets/Sprites/Towers/Attack", 256f),
            Rule.Ppu("Assets/Sprites/Towers/Fast/trumpeter", 256f), // trumpeter_* and trumpeters_*
            Rule.Ppu("Assets/Sprites/Towers/Slow", 256f),

            // Enemies, relative to a tower. All frames of one texture share one PPU.
            Rule.Size("Assets/Sprites/Enemies/Base", TowerSize * 2f / 3f), // swordsmen
            Rule.Size("Assets/Sprites/Enemies/Tank", TowerSize),           // heavy knight
            Rule.Size("Assets/Sprites/Enemies/Fast", TowerSize * 4f / 3f), // horseman, by length (long and thin, so twice the infantry)

            // Slow ring drawn under an enemy: one cell, so it stays visible around the knight too.
            Rule.Size("Assets/Sprites/Towers/Slow/slow_status", 1f),
        };

        [MenuItem(MenuPath)]
        public static void Normalize() => Run(apply: true);

        [MenuItem(ReportMenuPath)]
        private static void Report() => Run(apply: false);

        private static void Run(bool apply)
        {
            var linesByRule = new Dictionary<string, List<string>>();
            int changed = 0, total = 0;

            List<string> paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in paths)
                {
                    if (!TryGetRule(path, out Rule rule)) continue;
                    if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                    if (importer.textureType != TextureImporterType.Sprite) continue;

                    total++;
                    string line = ProcessTexture(path, importer, rule, apply);
                    if (line == null) continue;

                    changed++;
                    if (!linesByRule.TryGetValue(rule.PathPrefix, out List<string> lines))
                        linesByRule[rule.PathPrefix] = lines = new List<string>();
                    lines.Add(line);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            var report = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> pair in linesByRule)
            {
                foreach (string line in pair.Value.Take(MaxDetailedLinesPerRule))
                    report.AppendLine(line);
                if (pair.Value.Count > MaxDetailedLinesPerRule)
                    report.AppendLine($"...and {pair.Value.Count - MaxDetailedLinesPerRule} more in {pair.Key}");
            }

            string mode = apply ? "applied" : "report only, nothing changed";
            Debug.Log($"[SpriteSizeNormalizer] {changed} of {total} textures {(apply ? "updated" : "need changes")} ({mode}).\n{report}");
        }

        /// <summary>Returns a report line if the texture needs (or got) changes, otherwise null.</summary>
        private static string ProcessTexture(string path, TextureImporter importer, Rule rule, bool apply)
        {
            float oldPpu = importer.spritePixelsPerUnit;
            Vector2Int visible = GetVisibleSize(path, importer);
            float newPpu = rule.FixedPpu > 0f
                ? rule.FixedPpu
                : Mathf.Round(Mathf.Max(visible.x, visible.y) / rule.TargetUnits);

            bool pivotChanged = false;
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Center)
                {
                    pivotChanged = true;
                    if (apply)
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.Center;
                        importer.SetTextureSettings(settings);
                    }
                }
            }
            else
            {
                // Sprite Mode stays Multiple on purpose: switching to Single renames the sprite
                // (e.g. "cannon_flash_1_0" -> "cannon_flash_1") and breaks references in prefabs and configs.
                WarnIfPivotNotCentered(path);
            }

            bool ppuChanged = !Mathf.Approximately(oldPpu, newPpu);
            if (!ppuChanged && !pivotChanged) return null;

            if (apply)
            {
                importer.spritePixelsPerUnit = newPpu;
                importer.SaveAndReimport();
            }

            return $"{path}: PPU {oldPpu} -> {newPpu}, visible {visible.x}x{visible.y} px = " +
                   $"{visible.x / newPpu:F2}x{visible.y / newPpu:F2} units{(pivotChanged ? ", pivot -> Center" : "")}";
        }

        private static bool TryGetRule(string path, out Rule rule)
        {
            rule = default;
            int bestLength = -1;
            foreach (Rule candidate in Rules)
            {
                if (!path.StartsWith(candidate.PathPrefix) || candidate.PathPrefix.Length <= bestLength) continue;
                rule = candidate;
                bestLength = candidate.PathPrefix.Length;
            }
            return bestLength >= 0;
        }

        /// <summary>
        /// Size of the non-transparent part. For a sheet (Sprite Mode Multiple) it is the largest frame,
        /// so every frame gets the same PPU and the animation doesn't "breathe".
        /// </summary>
        private static Vector2Int GetVisibleSize(string path, TextureImporter importer)
        {
            // Read the PNG directly: the imported texture is usually not readable.
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                Color32[] pixels = texture.GetPixels32();
                int width = texture.width, height = texture.height;

                var rects = new List<RectInt>();
                if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (asset is Sprite sprite)
                        {
                            Rect r = sprite.rect;
                            rects.Add(new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height));
                        }
                    }
                }
                if (rects.Count == 0)
                    rects.Add(new RectInt(0, 0, width, height));

                Vector2Int max = Vector2Int.zero;
                foreach (RectInt rect in rects)
                {
                    Vector2Int size = AlphaBoundsSize(pixels, width, rect);
                    if (Mathf.Max(size.x, size.y) > Mathf.Max(max.x, max.y))
                        max = size;
                }
                return max;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Vector2Int AlphaBoundsSize(Color32[] pixels, int textureWidth, RectInt rect)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    if (pixels[y * textureWidth + x].a <= AlphaThreshold) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            return maxX < 0 ? Vector2Int.zero : new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        }

        private static void WarnIfPivotNotCentered(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is Sprite sprite)) continue;
                Vector2 normalized = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
                if ((normalized - new Vector2(0.5f, 0.5f)).sqrMagnitude > 0.0001f)
                    Debug.LogWarning($"[SpriteSizeNormalizer] {sprite.name} pivot is {normalized}, expected center. Fix it in the Sprite Editor.", asset);
            }
        }
    }
}
