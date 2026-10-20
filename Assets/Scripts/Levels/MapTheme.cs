using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Which tiles a map is drawn with. MapBuilder reads the layout from LevelConfig
    /// and the look from this asset, so the same level can be re-skinned without touching its data.
    /// </summary>
    [CreateAssetMenu(fileName = "MapTheme", menuName = "Tower Defense/Map Theme")]
    public sealed class MapTheme : ScriptableObject
    {
        [Header("Ground layer")]
        [Tooltip("Grass. A RoadEdgeTile, so cells next to the road get soft road edges.")]
        public TileBase grass;
        public TileBase road;
        public TileBase water;

        [Header("Build slots layer")]
        public TileBase buildSlot;

        [Header("Decoration layer")]
        [Tooltip("Variants are picked per cell, the same way every time the map is built.")]
        public TileBase[] trees;
        public TileBase[] rocks;
        public TileBase house;
        public TileBase castleWall;
    }
}
