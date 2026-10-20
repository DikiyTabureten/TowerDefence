using System.IO;
using TowerDefense.Levels;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Generates the three level assets in Assets/Configs/Levels. Levels are data: the layouts and waves
    /// below are the single source of truth, and re-running the menu item rewrites the assets in place
    /// (same GUIDs, so references to them survive). Balance numbers are a first guess — tune them here.
    /// Grid: 12x7 cells, (0,0) = bottom-left. Row 0 is mostly hidden behind the shop panel.
    /// </summary>
    public static class LevelGenerator
    {
        private const string MenuPath = "Tools/Tower Defense/Generate Sample Levels";
        private const string Folder = "Assets/Configs/Levels";
        private const string EnemiesFolder = "Assets/Configs/Enemies";

        private static EnemyConfig _swordsman, _horseman, _knight;

        private readonly struct Group
        {
            public readonly EnemyConfig Enemy;
            public readonly int Count;
            public readonly float Interval, Delay;
            public readonly int Path;

            public Group(EnemyConfig enemy, int count, float interval, float delay = 0f, int path = 0)
            {
                Enemy = enemy; Count = count; Interval = interval; Delay = delay; Path = path;
            }
        }

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);

            _swordsman = LoadEnemy("Enemy_Normal");
            _horseman = LoadEnemy("Enemy_Fast");
            _knight = LoadEnemy("Enemy_Tank");
            if (_swordsman == null || _horseman == null || _knight == null) return;

            VillageRoad();
            RiverCrossing();
            CastleSiege();

            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelGenerator] 3 levels generated in {Folder}.");
        }

        // 1. Tutorial: one winding road through a village. The knight only shows up in the last waves.
        private static void VillageRoad()
        {
            var so = Begin("Level_01_VillageRoad", "Village Road",
                "Tutorial: one winding road through a village. Heavy knights arrive in the last waves.",
                gold: 150, lives: 10, timeBetweenWaves: 5f);

            SetPaths(so, new[] { V(-1, 4), V(3, 4), V(3, 2), V(8, 2), V(8, 5), V(12, 5) });
            SetCells(so.FindProperty("_buildSlots"), V(2, 3), V(4, 3), V(6, 1), V(7, 3), V(9, 4), V(11, 4));
            SetTerrain(so,
                (TerrainType.Trees, new RectInt(0, 5, 2, 2)),
                (TerrainType.Trees, new RectInt(4, 5, 3, 2)),
                (TerrainType.Trees, new RectInt(10, 0, 2, 2)),
                (TerrainType.Houses, new RectInt(5, 4, 2, 1)),
                (TerrainType.Rocks, new RectInt(0, 2, 1, 1)),
                (TerrainType.Rocks, new RectInt(11, 2, 1, 1)));
            SetWaves(so,
                new[] { new Group(_swordsman, 6, 1f) },
                new[] { new Group(_swordsman, 8, 0.9f), new Group(_horseman, 4, 0.6f, delay: 4f) },
                new[] { new Group(_horseman, 10, 0.5f), new Group(_swordsman, 5, 1f, delay: 3f) },
                new[] { new Group(_swordsman, 8, 0.8f), new Group(_knight, 2, 3f, delay: 5f) },
                new[] { new Group(_knight, 3, 2.5f), new Group(_horseman, 10, 0.5f, delay: 2f), new Group(_swordsman, 8, 0.8f, delay: 6f) });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 2. Long straight roads along the river: swarms of fast cavalry. The Great Bell pays off here.
        private static void RiverCrossing()
        {
            var so = Begin("Level_02_RiverCrossing", "River Crossing",
                "Long straight roads along the river and swarms of fast cavalry. Slow them with the Great Bell.",
                gold: 175, lives: 10, timeBetweenWaves: 4f);

            SetPaths(so, new[] { V(-1, 5), V(6, 5), V(6, 2), V(12, 2) });
            SetCells(so.FindProperty("_buildSlots"), V(2, 4), V(4, 4), V(3, 6), V(5, 3), V(7, 4), V(8, 3), V(10, 3));
            SetTerrain(so,
                (TerrainType.Water, new RectInt(0, 0, 12, 2)),
                (TerrainType.Trees, new RectInt(8, 5, 4, 2)),
                (TerrainType.Trees, new RectInt(0, 6, 2, 1)),
                (TerrainType.Houses, new RectInt(2, 2, 2, 1)),
                (TerrainType.Rocks, new RectInt(7, 6, 1, 1)));
            SetWaves(so,
                new[] { new Group(_horseman, 8, 0.6f) },
                new[] { new Group(_horseman, 12, 0.45f), new Group(_swordsman, 4, 1.2f, delay: 3f) },
                new[] { new Group(_swordsman, 6, 0.9f), new Group(_horseman, 14, 0.4f, delay: 2f) },
                new[] { new Group(_horseman, 16, 0.35f), new Group(_knight, 1, 1f, delay: 6f) },
                new[] { new Group(_horseman, 20, 0.3f), new Group(_knight, 2, 3f, delay: 4f), new Group(_swordsman, 6, 0.8f, delay: 8f) });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 3. Finale: two roads meet before the castle gate; heavy waves come along both at once.
        private static void CastleSiege()
        {
            var so = Begin("Level_03_CastleSiege", "Castle Siege",
                "Two roads meet before the castle gate. Heavy waves on both roads: put the Orchestra next to the cannons.",
                gold: 250, lives: 15, timeBetweenWaves: 5f);

            SetPaths(so,
                new[] { V(-1, 5), V(4, 5), V(4, 3), V(12, 3) },   // 0: northern road
                new[] { V(-1, 1), V(7, 1), V(7, 3), V(12, 3) });  // 1: southern road, joins at (7,3)
            SetCells(so.FindProperty("_buildSlots"), V(2, 2), V(3, 4), V(5, 4), V(6, 2), V(8, 2), V(8, 4), V(9, 4));
            SetTerrain(so,
                (TerrainType.CastleWall, new RectInt(10, 0, 1, 7)), // the road through (10,3) is the gate
                (TerrainType.Houses, new RectInt(11, 0, 1, 3)),
                (TerrainType.Houses, new RectInt(11, 4, 1, 3)),
                (TerrainType.Trees, new RectInt(0, 6, 4, 1)),
                (TerrainType.Trees, new RectInt(5, 5, 4, 2)),
                (TerrainType.Rocks, new RectInt(0, 3, 1, 1)));
            SetWaves(so,
                new[] { new Group(_swordsman, 6, 1f, path: 0), new Group(_swordsman, 6, 1f, delay: 2f, path: 1) },
                new[] { new Group(_horseman, 8, 0.5f, path: 0), new Group(_swordsman, 8, 0.9f, path: 1) },
                new[] { new Group(_knight, 2, 3f, path: 0), new Group(_horseman, 10, 0.45f, delay: 2f, path: 1) },
                new[] { new Group(_knight, 3, 2.5f, path: 1), new Group(_swordsman, 10, 0.7f, path: 0), new Group(_horseman, 8, 0.5f, delay: 6f, path: 0) },
                new[] { new Group(_knight, 4, 2f, path: 0), new Group(_knight, 3, 2.5f, delay: 3f, path: 1),
                        new Group(_horseman, 12, 0.4f, delay: 6f, path: 1), new Group(_swordsman, 10, 0.6f, delay: 8f, path: 0) },
                new[] { new Group(_knight, 5, 1.8f, path: 0), new Group(_knight, 5, 1.8f, path: 1),
                        new Group(_horseman, 16, 0.35f, delay: 5f, path: 0), new Group(_horseman, 16, 0.35f, delay: 5f, path: 1) });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedObject Begin(string assetName, string displayName, string description,
            int gold, int lives, float timeBetweenWaves)
        {
            string path = $"{Folder}/{assetName}.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(level, path);
            }

            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_gridSize").vector2IntValue = new Vector2Int(12, 7);
            so.FindProperty("_tileSize").floatValue = 1f;
            so.FindProperty("_startingGold").intValue = gold;
            so.FindProperty("_startingLives").intValue = lives;
            so.FindProperty("_timeBetweenWaves").floatValue = timeBetweenWaves;
            return so;
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        private static void SetPaths(SerializedObject so, params Vector2Int[][] paths)
        {
            SerializedProperty list = so.FindProperty("_paths");
            list.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
                SetCells(list.GetArrayElementAtIndex(i).FindPropertyRelative("waypoints"), paths[i]);
        }

        private static void SetCells(SerializedProperty list, params Vector2Int[] cells)
        {
            list.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
                list.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
        }

        private static void SetTerrain(SerializedObject so, params (TerrainType type, RectInt area)[] areas)
        {
            SerializedProperty list = so.FindProperty("_terrain");
            list.arraySize = areas.Length;
            for (int i = 0; i < areas.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("type").enumValueIndex = (int)areas[i].type;
                element.FindPropertyRelative("area").rectIntValue = areas[i].area;
            }
        }

        private static void SetWaves(SerializedObject so, params Group[][] waves)
        {
            SerializedProperty list = so.FindProperty("_waves");
            list.arraySize = waves.Length;
            for (int w = 0; w < waves.Length; w++)
            {
                SerializedProperty groups = list.GetArrayElementAtIndex(w).FindPropertyRelative("groups");
                groups.arraySize = waves[w].Length;
                for (int g = 0; g < waves[w].Length; g++)
                {
                    Group group = waves[w][g];
                    SerializedProperty element = groups.GetArrayElementAtIndex(g);
                    element.FindPropertyRelative("enemy").objectReferenceValue = group.Enemy;
                    element.FindPropertyRelative("count").intValue = group.Count;
                    element.FindPropertyRelative("spawnInterval").floatValue = group.Interval;
                    element.FindPropertyRelative("delayBeforeGroup").floatValue = group.Delay;
                    element.FindPropertyRelative("pathIndex").intValue = group.Path;
                }
            }
        }

        private static EnemyConfig LoadEnemy(string name)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"{EnemiesFolder}/{name}.asset");
            if (enemy == null) Debug.LogError($"[LevelGenerator] Enemy config not found: {EnemiesFolder}/{name}.asset");
            return enemy;
        }
    }
}
