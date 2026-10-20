using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Replaces the 64 px tower/FX/projectile textures with their 4x (256 px) versions from the art pack.
    /// Files are overwritten in place, so names, .meta and GUIDs stay the same and no reference breaks.
    /// Sliced textures (Sprite Mode Multiple) get their frame rectangles scaled by the same factor,
    /// keeping the frame IDs. Finally the scale standard is re-applied (SpriteSizeNormalizer),
    /// so every object keeps its size in world units — it just gets sharper.
    /// </summary>
    public static class HdSpriteImporter
    {
        private const string MenuPath = "Tools/Tower Defense/Import HD Tower Sprites (256)";
        private const string SourceRoot = "_incoming/images/Art"; // unpacked art pack, outside Assets

        // Project texture <- source file in the art pack.
        private static readonly (string target, string source)[] Files =
        {
            ("Assets/Sprites/Towers/Attack/cannon_base.png", "Towers/Cannon/png256/cannon_base.png"),
            ("Assets/Sprites/Towers/Attack/cannon_turret.png", "Towers/Cannon/png256/cannon_turret.png"),
            ("Assets/Sprites/Towers/Attack/cannon_full.png", "Towers/Cannon/png256/cannon_full.png"),
            ("Assets/Sprites/Towers/Attack/cannon_flash_1.png", "Towers/Cannon/png256/cannon_flash_1.png"),
            ("Assets/Sprites/Towers/Attack/cannon_flash_2.png", "Towers/Cannon/png256/cannon_flash_2.png"),
            ("Assets/Sprites/Towers/Attack/cannonball_png64.png", "Projectiles/png256/cannonball.png"), // old name kept on purpose
            ("Assets/Sprites/Towers/Attack/smoke_puff.png", "FX/png256/smoke_puff.png"),

            ("Assets/Sprites/Towers/Fast/trumpeters_base.png", "Towers/Orchestra/png256/trumpeters_base.png"),
            ("Assets/Sprites/Towers/Fast/trumpeters_full.png", "Towers/Orchestra/png256/trumpeters_full.png"),
            ("Assets/Sprites/Towers/Fast/trumpeter_red.png", "Towers/Orchestra/png256/trumpeter_red.png"),
            ("Assets/Sprites/Towers/Fast/trumpeter_blue.png", "Towers/Orchestra/png256/trumpeter_blue.png"),
            ("Assets/Sprites/Towers/Fast/trumpeter_orange.png", "Towers/Orchestra/png256/trumpeter_orange.png"),
            ("Assets/Sprites/Towers/Fast/note_beamed.png", "Towers/Orchestra/png256/note_beamed.png"),
            ("Assets/Sprites/Towers/Fast/note_eighth.png", "Towers/Orchestra/png256/note_eighth.png"),
            ("Assets/Sprites/Towers/Fast/note_quarter.png", "Towers/Orchestra/png256/note_quarter.png"),

            ("Assets/Sprites/Towers/Slow/bell_base.png", "Towers/Bell/png256/bell_base.png"),
            ("Assets/Sprites/Towers/Slow/bell_full.png", "Towers/Bell/png256/bell_full.png"),
            ("Assets/Sprites/Towers/Slow/bell_body.png", "Towers/Bell/png256/bell_body.png"),
            ("Assets/Sprites/Towers/Slow/bell_headstock.png", "Towers/Bell/png256/bell_headstock.png"),
            ("Assets/Sprites/Towers/Slow/bell_ringwave.png", "Towers/Bell/png256/bell_ringwave.png"),
            ("Assets/Sprites/Towers/Slow/bell_ringwave 1.png", "Towers/Bell/png256/bell_ringwave.png"), // second wave uses a copy
            ("Assets/Sprites/Towers/Slow/slow_status.png", "FX/png256/slow_status.png"),
        };

        [MenuItem(MenuPath)]
        private static void Import()
        {
            int replaced = 0;
            foreach ((string target, string source) in Files)
            {
                if (Replace(target, Path.Combine(SourceRoot, source)))
                    replaced++;
            }

            SpriteSizeNormalizer.Normalize();
            Debug.Log($"[HdSpriteImporter] Replaced {replaced} of {Files.Length} textures.");
        }

        private static bool Replace(string targetPath, string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[HdSpriteImporter] Source not found: {sourcePath}");
                return false;
            }
            if (!(AssetImporter.GetAtPath(targetPath) is TextureImporter importer))
            {
                Debug.LogError($"[HdSpriteImporter] Target is not a texture in the project: {targetPath}");
                return false;
            }

            importer.GetSourceTextureWidthAndHeight(out int oldWidth, out _);
            SpriteRect[] oldRects = importer.spriteImportMode == SpriteImportMode.Multiple
                ? GetProvider(importer).GetSpriteRects()
                : null;

            File.Copy(sourcePath, targetPath, overwrite: true);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            importer = (TextureImporter)AssetImporter.GetAtPath(targetPath);
            importer.GetSourceTextureWidthAndHeight(out int newWidth, out _);

            if (oldRects != null && oldRects.Length > 0 && newWidth != oldWidth)
            {
                // Same picture, just more pixels: scale every frame rectangle, keep names and IDs.
                float factor = (float)newWidth / oldWidth;
                foreach (SpriteRect rect in oldRects)
                {
                    Rect r = rect.rect;
                    rect.rect = new Rect(r.x * factor, r.y * factor, r.width * factor, r.height * factor);
                }

                ISpriteEditorDataProvider provider = GetProvider(importer);
                provider.SetSpriteRects(oldRects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                    .SetNameFileIdPairs(oldRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                provider.Apply();
                importer.SaveAndReimport();
            }

            return true;
        }

        private static ISpriteEditorDataProvider GetProvider(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            return provider;
        }
    }
}
