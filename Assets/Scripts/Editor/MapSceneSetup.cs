using TowerDefense.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Adds the map to the open scene: a "Map" Grid with three Tilemaps and a MapBuilder,
    /// then builds level 1 with the default theme. Also prepares level loading at runtime:
    /// the BuildZone prefab, a "BuildZones" parent and the Bootstrap references.
    /// Re-running reuses the existing objects.
    /// The map is drawn below everything else (negative Order in Layer); gameplay sprites use 0 and above.
    /// </summary>
    public static class MapSceneSetup
    {
        private const string MenuPath = "Tools/Tower Defense/Setup Map In Scene";
        private const string LevelPath = "Assets/Configs/Levels/Level_01_VillageRoad.asset";
        private const string ThemePath = "Assets/Configs/MapTheme_Default.asset";
        private const string BuildZonePrefabPath = "Assets/Prefabs/BuildZone.prefab";

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

            BuildZone zonePrefab = GetOrCreateBuildZonePrefab();
            Transform zonesRoot = GetOrCreateRoot("BuildZones");
            Transform pathsRoot = GetOrCreateRoot("Paths");
            WireBootstrap(level, builder, zonePrefab, zonesRoot, pathsRoot);
            DisableManualBuildZones(zonesRoot);

            EditorSceneManager.MarkSceneDirty(map.scene);
            EditorSceneManager.SaveScene(map.scene);
            Debug.Log($"[MapSceneSetup] Map built for '{level.DisplayName}' and scene saved.", map);
        }

        /// <summary>
        /// Invisible build zone: just the collider on the BuildZone layer. The slot is shown by the
        /// stone pad the MapBuilder draws on the BuildSlots tilemap, so the zone needs no sprite.
        /// </summary>
        private static BuildZone GetOrCreateBuildZonePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BuildZone>(BuildZonePrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("BuildZone");
            try
            {
                go.layer = LayerMask.NameToLayer("BuildZone");
                var box = go.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
                box.isTrigger = true;
                go.AddComponent<BuildZone>();
                return PrefabUtility.SaveAsPrefabAsset(go, BuildZonePrefabPath).GetComponent<BuildZone>();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                root = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(root, $"Create {name}");
            }
            return root.transform;
        }

        private static void WireBootstrap(LevelConfig level, MapBuilder builder, BuildZone zonePrefab,
            Transform zonesRoot, Transform pathsRoot)
        {
            var bootstrap = Object.FindFirstObjectByType<Bootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[MapSceneSetup] No Bootstrap in the scene: assign Level, Map Builder and Build Zone fields by hand.");
                return;
            }

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_mapBuilder").objectReferenceValue = builder;
            so.FindProperty("_buildZonePrefab").objectReferenceValue = zonePrefab;
            so.FindProperty("_buildZonesRoot").objectReferenceValue = zonesRoot;
            so.FindProperty("_pathsRoot").objectReferenceValue = pathsRoot;
            if (so.FindProperty("_defaultLevel").objectReferenceValue == null)
                so.FindProperty("_defaultLevel").objectReferenceValue = level;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Zones placed by hand in the scene would still accept towers at old positions.
        /// They are only switched off (not deleted), so nothing is lost.
        /// </summary>
        private static void DisableManualBuildZones(Transform zonesRoot)
        {
            foreach (BuildZone zone in Object.FindObjectsByType<BuildZone>(FindObjectsSortMode.None))
            {
                if (zone.transform.IsChildOf(zonesRoot)) continue;
                Undo.RecordObject(zone.gameObject, "Disable manual build zone");
                zone.gameObject.SetActive(false);
                Debug.Log($"[MapSceneSetup] Disabled manual build zone '{zone.name}' (zones now come from LevelConfig).", zone);
            }
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
