using System;
using UnityEngine;

namespace TowerDefense
{
    public enum PlacementResult
    {
        Success,
        NotEnoughGold,
        OutsideBuildZone,
        CellOccupied
    }

    /// <summary>
    /// Owns every placement rule. The UI only asks it to place a tower;
    /// it checks gold, build zone and occupancy, spends gold and calls TowerFactory.
    /// </summary>
    public sealed class TowerPlacementService
    {
        private readonly PlayerWallet _wallet;
        private readonly TowerFactory _factory;
        private readonly PlacementGrid _grid;
        private readonly LayerMask _buildZoneMask;

        public event Action<Tower> TowerPlaced;

        public TowerPlacementService(PlayerWallet wallet, TowerFactory factory, PlacementGrid grid, LayerMask buildZoneMask)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _buildZoneMask = buildZoneMask;
        }

        /// <summary>World position where a tower dropped at worldPosition would actually stand.</summary>
        public Vector3 GetSnappedPosition(Vector2 worldPosition)
        {
            return _grid.CellToWorld(_grid.WorldToCell(worldPosition));
        }

        public PlacementResult Validate(TowerConfig config, Vector2 worldPosition)
        {
            return Validate(config, worldPosition, out _);
        }

        public PlacementResult TryPlace(TowerConfig config, Vector2 worldPosition)
        {
            PlacementResult result = Validate(config, worldPosition, out Vector2Int cell);
            if (result != PlacementResult.Success) return result;

            // Validate already checked gold, but TrySpend is the authoritative check.
            if (!_wallet.TrySpend(config.Cost)) return PlacementResult.NotEnoughGold;

            Tower tower = _factory.Create(config, _grid.CellToWorld(cell));
            _grid.Occupy(cell, tower);
            TowerPlaced?.Invoke(tower);

            return PlacementResult.Success;
        }

        private PlacementResult Validate(TowerConfig config, Vector2 worldPosition, out Vector2Int cell)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            cell = _grid.WorldToCell(worldPosition);

            if (!_wallet.CanAfford(config.Cost))
                return PlacementResult.NotEnoughGold;

            if (Physics2D.OverlapPoint(_grid.CellToWorld(cell), _buildZoneMask) == null)
                return PlacementResult.OutsideBuildZone;

            if (_grid.IsOccupied(cell))
                return PlacementResult.CellOccupied;

            return PlacementResult.Success;
        }
    }
}
