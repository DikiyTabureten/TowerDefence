using System.Collections.Generic;
using System.IO;
using System.Linq;
using TowerDefense.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// Builds the default map theme from the Kenney "Tower Defense (top-down)" Retina tiles:
    /// Tile assets, the grass RoadEdgeTile with its rules, and Assets/Configs/MapTheme_Default.asset.
    /// Safe to run again: existing assets are updated in place, so references stay valid.
    /// </summary>
    public static class MapThemeCreator
    {
        private const string MenuPath = "Tools/Tower Defense/Create Map Theme";
        private const string SpriteFolder = "Assets/Sprites/Map/Retina";
        private const string TileFolder = "Assets/Tiles/Map";
        private const string ThemePath = "Assets/Configs/MapTheme_Default.asset";

        // Kenney file numbers (towerDefense_tileNNN.png).
        private const int Grass = 124;      // flat grass, matches the grass in the edge tiles
        private const int Road = 93;        // plain dirt
        private const int Water = 159;      // grey stone, tinted blue (the pack has no water)
        private const int BuildSlot = 180;  // stone pad, transparent around it
        private static readonly int[] Trees = { 130, 131 };
        private static readonly int[] Rocks = { 135, 136, 137 };
        private const int House = 182;
        private const int CastleWall = 229;

        private static readonly Color WaterTint = new Color(0.55f, 0.8f, 1f);

        // Neighbour offsets on a Tilemap (y points up).
        private static readonly Vector3Int N = new Vector3Int(0, 1, 0), S = new Vector3Int(0, -1, 0),
            E = new Vector3Int(1, 0, 0), W = new Vector3Int(-1, 0, 0),
            NE = new Vector3Int(1, 1, 0), NW = new Vector3Int(-1, 1, 0),
            SE = new Vector3Int(1, -1, 0), SW = new Vector3Int(-1, -1, 0);

        private const int R = RoadEdgeTile.Neighbor.Road;
        private const int X = RoadEdgeTile.Neighbor.NotRoad;

        [MenuItem(MenuPath)]
        public static void Create()
        {
            SpriteSizeNormalizer.Normalize(); // Retina tiles: PPU 128, one tile = one cell
            Directory.CreateDirectory(TileFolder);

            Tile road = CreateTile("Road", Road, Color.white);
            Tile water = CreateTile("Water", Water, WaterTint);
            Tile buildSlot = CreateTile("BuildSlot", BuildSlot, Color.white);
            Tile[] trees = Trees.Select((n, i) => CreateTile($"Tree_{i + 1}", n, Color.white)).ToArray();
            Tile[] rocks = Rocks.Select((n, i) => CreateTile($"Rock_{i + 1}", n, Color.white)).ToArray();
            Tile house = CreateTile("House", House, Color.white);
            Tile castleWall = CreateTile("CastleWall", CastleWall, Color.white);
            RoadEdgeTile grass = CreateGrass(road);

            MapTheme theme = LoadOrCreate<MapTheme>(ThemePath);
            theme.grass = grass;
            theme.road = road;
            theme.water = water;
            theme.buildSlot = buildSlot;
            theme.trees = trees;
            theme.rocks = rocks;
            theme.house = house;
            theme.castleWall = castleWall;
            EditorUtility.SetDirty(theme);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MapThemeCreator] Map theme ready: {ThemePath}, tiles in {TileFolder}.", theme);
        }

        private static RoadEdgeTile CreateGrass(TileBase road)
        {
            RoadEdgeTile grass = LoadOrCreate<RoadEdgeTile>($"{TileFolder}/Grass_RoadEdge.asset");
            grass.road = road;
            grass.m_DefaultSprite = LoadSprite(Grass);
            grass.m_DefaultColliderType = Tile.ColliderType.None;

            // Order matters: the first matching rule wins. "R" = road there, "X" = no road there.
            grass.m_TilingRules = new List<RuleTile.TilingRule>
            {
                // Squeezed between two roads: the pack has no tile for that, so it becomes road too.
                Rule(Road, (N, R), (S, R)),
                Rule(Road, (E, R), (W, R)),
                // Inner corners of a bend: road on two adjacent sides.
                Rule(73, (N, R), (E, R), (S, X), (W, X)),
                Rule(72, (N, R), (W, R), (S, X), (E, X)),
                Rule(96, (S, R), (E, R), (N, X), (W, X)),
                Rule(95, (S, R), (W, R), (N, X), (E, X)),
                // Straight edges: road on one side.
                Rule(116, (N, R), (S, X), (E, X), (W, X)),
                Rule(70, (S, R), (N, X), (E, X), (W, X)),
                Rule(92, (E, R), (N, X), (S, X), (W, X)),
                Rule(94, (W, R), (N, X), (S, X), (E, X)),
                // Outer corners: road only diagonally.
                Rule(115, (NE, R), (N, X), (E, X)),
                Rule(117, (NW, R), (N, X), (W, X)),
                Rule(69, (SE, R), (S, X), (E, X)),
                Rule(71, (SW, R), (S, X), (W, X)),
            };

            EditorUtility.SetDirty(grass);
            return grass;
        }

        private static RuleTile.TilingRule Rule(int sprite, params (Vector3Int offset, int neighbor)[] neighbors)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Sprites = new[] { LoadSprite(sprite) },
                m_ColliderType = Tile.ColliderType.None
            };
            rule.ApplyNeighbors(neighbors.ToDictionary(n => n.offset, n => n.neighbor));
            return rule;
        }

        private static Tile CreateTile(string name, int sprite, Color color)
        {
            Tile tile = LoadOrCreate<Tile>($"{TileFolder}/{name}.asset");
            tile.sprite = LoadSprite(sprite);
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Sprite LoadSprite(int number)
        {
            string path = $"{SpriteFolder}/towerDefense_tile{number:000}.png";
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
                Debug.LogError($"[MapThemeCreator] Sprite not found: {path}. Copy it from the Kenney pack (PNG/Retina).");
            return sprite;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
