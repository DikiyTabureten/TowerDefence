using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Grass Rule Tile that knows about the road. Kenney's road edges are drawn on the grass
    /// next to the road, so each grass cell picks its sprite from which neighbours are road
    /// (edge, inner corner of a bend, outer corner) — the rules are created by "Create Map Theme".
    /// Plain RuleTile can only tell "this tile / not this tile"; this adds "road / not road",
    /// so water or the map border next to grass does not produce road edges.
    /// </summary>
    [CreateAssetMenu(fileName = "RoadEdgeTile", menuName = "Tower Defense/Road Edge Tile")]
    public sealed class RoadEdgeTile : RuleTile<RoadEdgeTile.Neighbor>
    {
        [Tooltip("The road tile the edges react to.")]
        public TileBase road;

        public sealed class Neighbor : RuleTile.TilingRuleOutput.Neighbor
        {
            public const int Road = 3;
            public const int NotRoad = 4;
        }

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            switch (neighbor)
            {
                case Neighbor.Road: return other != null && other == road;
                case Neighbor.NotRoad: return other == null || other != road;
            }
            return base.RuleMatch(neighbor, other);
        }
    }
}
