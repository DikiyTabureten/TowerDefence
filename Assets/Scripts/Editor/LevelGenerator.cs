using System.Collections.Generic;
using System.IO;
using TowerDefense.Levels;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Generates the level assets in Assets/Configs/Levels. Levels are data: the layout below
    /// is the single source of truth, and re-running the menu item rewrites the assets in place
    /// (same GUIDs, so references to them survive).
    /// Grid: 12x7 cells, (0,0) = bottom-left. Row 0 is mostly hidden behind the shop panel.
    /// </summary>
    public static class LevelGenerator
    {
        private const string MenuPath = "Tools/Tower Defense/Generate Sample Levels";
        private const string Folder = "Assets/Configs/Levels";
        private const string WavesFolder = "Assets/Configs/Waves";

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            LevelConfig level = GenerateVillageRoad();
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelGenerator] Levels generated in {Folder}.", level);
        }

        private static LevelConfig GenerateVillageRoad()
        {
            LevelConfig level = LoadOrCreate($"{Folder}/Level_01_VillageRoad.asset");
            var so = new SerializedObject(level);

            so.FindProperty("_displayName").stringValue = "Village Road";
            so.FindProperty("_description").stringValue = "Tutorial: one winding road through a village.";
            so.FindProperty("_gridSize").vector2IntValue = new Vector2Int(12, 7);
            so.FindProperty("_tileSize").floatValue = 1f;

            // Enemies walk in from the left edge and leave on the right.
            SetPaths(so, new[]
            {
                new[] { V(-1, 4), V(3, 4), V(3, 2), V(8, 2), V(8, 5), V(12, 5) }
            });

            // At the bends and along the long middle stretch.
            SetCells(so.FindProperty("_buildSlots"), V(2, 3), V(4, 3), V(6, 1), V(7, 3), V(9, 4), V(11, 4));

            SetTerrain(so,
                (TerrainType.Trees, new RectInt(0, 5, 2, 2)),
                (TerrainType.Trees, new RectInt(4, 5, 3, 2)),
                (TerrainType.Trees, new RectInt(10, 0, 2, 2)),
                (TerrainType.Houses, new RectInt(5, 4, 2, 1)),
                (TerrainType.Rocks, new RectInt(0, 2, 1, 1)),
                (TerrainType.Rocks, new RectInt(11, 2, 1, 1)));

            so.FindProperty("_startingGold").intValue = 150;
            so.FindProperty("_startingLives").intValue = 10;
            so.FindProperty("_timeBetweenWaves").floatValue = 5f;
            SetWaves(so, "Wave_01", "Wave_02", "Wave_03");

            so.ApplyModifiedPropertiesWithoutUndo();
            return level;
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        private static void SetPaths(SerializedObject so, Vector2Int[][] paths)
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

        private static void SetWaves(SerializedObject so, params string[] waveNames)
        {
            var waves = new List<WaveConfig>();
            foreach (string name in waveNames)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveConfig>($"{WavesFolder}/{name}.asset");
                if (wave != null) waves.Add(wave);
                else Debug.LogWarning($"[LevelGenerator] Wave not found: {WavesFolder}/{name}.asset");
            }

            SerializedProperty list = so.FindProperty("_waves");
            list.arraySize = waves.Count;
            for (int i = 0; i < waves.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
        }

        private static LevelConfig LoadOrCreate(string path)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (level != null) return level;

            level = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(level, path);
            return level;
        }
    }
}
