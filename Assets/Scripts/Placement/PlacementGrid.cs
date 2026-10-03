using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>Snaps world positions to cells and remembers which cells already hold a tower.</summary>
    public sealed class PlacementGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector2Int, Tower> _occupied = new Dictionary<Vector2Int, Tower>();

        public PlacementGrid(float cellSize)
        {
            _cellSize = Mathf.Max(0.01f, cellSize);
        }

        public Vector2Int WorldToCell(Vector2 world)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x / _cellSize), Mathf.FloorToInt(world.y / _cellSize));
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * _cellSize, (cell.y + 0.5f) * _cellSize, 0f);
        }

        public bool IsOccupied(Vector2Int cell)
        {
            // A destroyed tower no longer occupies its cell.
            if (_occupied.TryGetValue(cell, out Tower tower) && tower != null) return true;

            _occupied.Remove(cell);
            return false;
        }

        public void Occupy(Vector2Int cell, Tower tower) => _occupied[cell] = tower;

        public void Release(Vector2Int cell) => _occupied.Remove(cell);
    }
}
