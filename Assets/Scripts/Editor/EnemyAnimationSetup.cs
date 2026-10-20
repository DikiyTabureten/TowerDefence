using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// One-click setup of enemy walk animations:
    /// 1) slices each walk sheet into 8 square frames "&lt;sheet&gt;_0..7";
    /// 2) applies the scale standard (SpriteSizeNormalizer) so all frames of a sheet share one PPU;
    /// 3) fills EnemyConfig: Sprite, Walk Frames, Distance Per Cycle, Emits Dust, facing;
    /// 4) completes the shared Enemy prefab: EnemyWalkAnimator, SlowStatusView, SlowRing and Dust children.
    /// Safe to run again: existing frames keep their IDs, so references don't break.
    /// </summary>
    public static class EnemyAnimationSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Enemy Animations";
        private const int FramesPerSheet = 8;

        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string DustSourcePrefabPath = "Assets/Prefabs/ShooterTower.prefab"; // reuse the cannon smoke setup
        private const string DustSourceChild = "Turret/Smoke";
        private const string SlowRingSpritePath = "Assets/Sprites/Towers/Slow/slow_status.png";

        private readonly struct EnemyAnimation
        {
            public readonly string ConfigPath;
            public readonly string SheetPath;
            public readonly float DistancePerCycle; // world units per full walk cycle
            public readonly bool EmitsDust;

            public EnemyAnimation(string configPath, string sheetPath, float distancePerCycle, bool emitsDust)
            {
                ConfigPath = configPath;
                SheetPath = sheetPath;
                DistancePerCycle = distancePerCycle;
                EmitsDust = emitsDust;
            }
        }

        // Distance Per Cycle scales with the enemy's size (see SpriteSizeNormalizer):
        // a bigger body takes longer steps, otherwise its legs would "slide" over the ground.
        private static readonly EnemyAnimation[] Enemies =
        {
            new EnemyAnimation("Assets/Configs/Enemies/Enemy_Normal.asset",
                "Assets/Sprites/Enemies/Base/enemy_swordsman_shield_walk_256.png", 0.61f, false), // ~0.61 units
            new EnemyAnimation("Assets/Configs/Enemies/Enemy_Tank.asset",
                "Assets/Sprites/Enemies/Tank/enemy_knight_walk_256.png", 0.85f, false),           // ~0.92 units
            new EnemyAnimation("Assets/Configs/Enemies/Enemy_Fast.asset",
                "Assets/Sprites/Enemies/Fast/enemy_horseman_walk_256.png", 1.65f, true),          // ~1.24 units long
        };

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            foreach (EnemyAnimation enemy in Enemies)
                SliceSheet(enemy.SheetPath);

            SpriteSizeNormalizer.Normalize();

            foreach (EnemyAnimation enemy in Enemies)
                FillConfig(enemy);

            SetupEnemyPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyAnimationSetup] Done: sheets sliced, configs filled, Enemy prefab updated.");
        }

        private static void SliceSheet(string sheetPath)
        {
            if (!(AssetImporter.GetAtPath(sheetPath) is TextureImporter importer))
            {
                Debug.LogError($"[EnemyAnimationSetup] Sheet not found: {sheetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Keep the IDs of frames that already exist, so prefabs/configs referencing them stay valid.
            Dictionary<string, GUID> existingIds = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int cell = width / FramesPerSheet;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(sheetPath);

            var rects = new SpriteRect[FramesPerSheet];
            for (int i = 0; i < FramesPerSheet; i++)
            {
                string name = $"{baseName}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * cell, 0, cell, height),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = existingIds.TryGetValue(name, out GUID id) ? id : GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void FillConfig(EnemyAnimation enemy)
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(enemy.ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[EnemyAnimationSetup] Config not found: {enemy.ConfigPath}");
                return;
            }

            List<Sprite> frames = AssetDatabase.LoadAllAssetsAtPath(enemy.SheetPath)
                .OfType<Sprite>()
                .OrderBy(FrameIndex)
                .ToList();
            if (frames.Count != FramesPerSheet)
            {
                Debug.LogError($"[EnemyAnimationSetup] {enemy.SheetPath}: expected {FramesPerSheet} frames, found {frames.Count}.");
                return;
            }

            var so = new SerializedObject(config);
            so.FindProperty("_sprite").objectReferenceValue = frames[0];
            SerializedProperty walkFrames = so.FindProperty("_walkFrames");
            walkFrames.arraySize = frames.Count;
            for (int i = 0; i < frames.Count; i++)
                walkFrames.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.FindProperty("_distancePerCycle").floatValue = enemy.DistancePerCycle;
            so.FindProperty("_emitsDust").boolValue = enemy.EmitsDust;
            so.FindProperty("_facingMode").enumValueIndex = (int)EnemyFacingMode.RotateTowardsWaypoint;
            so.FindProperty("_spriteFacingAngle").floatValue = 0f; // all sheets are drawn facing right
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int FrameIndex(Sprite sprite)
        {
            int underscore = sprite.name.LastIndexOf('_');
            return int.TryParse(sprite.name.Substring(underscore + 1), out int index) ? index : int.MaxValue;
        }

        private static void SetupEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                var body = root.GetComponent<SpriteRenderer>();
                var enemy = root.GetComponent<Enemy>();

                SpriteRenderer ring = GetOrCreateSlowRing(root.transform);
                ParticleSystem dust = GetOrCreateDust(root.transform);

                var walk = GetOrAdd<EnemyWalkAnimator>(root);
                var slow = GetOrAdd<SlowStatusView>(root);

                SetReference(enemy, "_spriteRenderer", body);
                SetReference(walk, "_renderer", body);
                SetReference(walk, "_dust", dust);
                SetReference(slow, "_ring", ring);

                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer GetOrCreateSlowRing(Transform parent)
        {
            Transform ringTransform = parent.Find("SlowRing");
            if (ringTransform == null)
            {
                ringTransform = new GameObject("SlowRing").transform;
                ringTransform.SetParent(parent, false);
            }

            var ring = GetOrAdd<SpriteRenderer>(ringTransform.gameObject);
            ring.sprite = AssetDatabase.LoadAllAssetsAtPath(SlowRingSpritePath).OfType<Sprite>().FirstOrDefault();
            ring.sortingOrder = -1; // under the enemy
            ring.enabled = false;   // SlowStatusView turns it on while the enemy is slowed
            return ring;
        }

        private static ParticleSystem GetOrCreateDust(Transform parent)
        {
            Transform dustTransform = parent.Find("Dust");
            if (dustTransform == null)
            {
                // Copy the cannon smoke: it already has a working URP material and the smoke_puff sprite.
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(DustSourcePrefabPath).transform.Find(DustSourceChild);
                dustTransform = Object.Instantiate(source.gameObject, parent, false).transform;
                dustTransform.name = "Dust";
            }

            // Behind the enemy (sprites face right, so "behind" is -X).
            dustTransform.localPosition = new Vector3(-0.5f, 0f, 0f);
            dustTransform.localRotation = Quaternion.identity;
            dustTransform.localScale = Vector3.one;

            var dust = dustTransform.GetComponent<ParticleSystem>();

            ParticleSystem.MainModule main = dust.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // puffs stay where they were kicked up
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(0xC9 / 255f, 0xB4 / 255f, 0x8A / 255f, 0.7f);
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = dust.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 8f; // more running = more dust; standing still = none
            emission.SetBursts(new ParticleSystem.Burst[0]);
            emission.enabled = false;       // EnemyWalkAnimator enables it only for configs with Emits Dust

            ParticleSystem.ShapeModule shape = dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;
            shape.rotation = Vector3.zero;

            ParticleSystem.ColorOverLifetimeModule color = dust.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            dust.GetComponent<ParticleSystemRenderer>().sortingOrder = -2; // under the ring and the enemy
            return dust;
        }

        // Not "GetComponent() ?? AddComponent()": Unity's fake-null objects break the ?? operator.
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
