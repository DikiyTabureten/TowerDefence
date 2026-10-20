using TowerDefense.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Adds the map to the open scene: a "Map" Grid with three Tilemaps and a MapBuilder,
    /// then builds level 1 with the default theme. Re-running reuses the existing objects.
    /// The map is drawn below everything else (negative Order in Layer); gameplay sprites use 0 and above.
    /// </summary>
    public static class MapSceneSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Map In Scene";
        private const string LevelPath = "Assets/Configs/Levels/Level_01_VillageRoad.asset";
        private const string ThemePath = "Assets/Configs/MapTheme_Default.asset";

        // Sorting orders: ground < build slots < decoration < build zones (-1), enemies (0), towers, projectiles.
        private const int GroundOrder = -30;
        private const int BuildSlotsOrder = -20;
        private const int DecorationOrder = -10;

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPath);
            var theme = AssetDatabase.LoadAssetAtPath<MapTheme>(ThemePath);
            if (level == null || theme == null)
            {
                Debug.LogError("[MapSceneSetup] Run 'Generate Sample Levels' and 'Create Map Theme' first.");
                return;
            }

            GameObject map = GameObject.Find("Map");
            if (map == null) map = CreateGrid();
            Tilemap ground = GetOrCreateTilemap(map.transform, "Ground", GroundOrder);
            Tilemap buildSlots = GetOrCreateTilemap(map.transform, "BuildSlots", BuildSlotsOrder);
            Tilemap decoration = GetOrCreateTilemap(map.transform, "Decoration", DecorationOrder);

            MapBuilder builder = map.GetComponent<MapBuilder>();
            if (builder == null) builder = Undo.AddComponent<MapBuilder>(map);

            var so = new SerializedObject(builder);
            so.FindProperty("_level").objectReferenceValue = level;
            so.FindProperty("_theme").objectReferenceValue = theme;
            so.FindProperty("_ground").objectReferenceValue = ground;
            so.FindProperty("_buildSlots").objectReferenceValue = buildSlots;
            so.FindProperty("_decoration").objectReferenceValue = decoration;
            so.ApplyModifiedProperties();

            builder.Build();

            EditorSceneManager.MarkSceneDirty(map.scene);
            EditorSceneManager.SaveScene(map.scene);
            Debug.Log($"[MapSceneSetup] Map built for '{level.DisplayName}' and scene saved.", map);
        }

        private static GameObject CreateGrid()
        {
            var map = new GameObject("Map", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(map, "Create Map");
            return map;
        }

        private static Tilemap GetOrCreateTilemap(Transform parent, string name, int sortingOrder)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                Undo.RegisterCreatedObjectUndo(go, "Create Tilemap");
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            child.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return child.GetComponent<Tilemap>();
        }
    }
}
