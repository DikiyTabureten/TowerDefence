using System;
using UnityEngine;

namespace TowerDefense
{
    public enum PlacementResult
    {
        Success,
        NotEnoughGold,
        OutsideBuildZone,
        ZoneOccupied
    }

    /// <summary>
    /// Owns every placement rule. The UI only asks it to place a tower;
    /// it checks gold, finds the build zone under the cursor, checks that the zone is free,
    /// spends gold and calls TowerFactory. One build zone holds exactly one tower.
    /// </summary>
    public sealed class TowerPlacementService
    {
        private readonly PlayerWallet _wallet;
        private readonly TowerFactory _factory;
        private readonly LayerMask _buildZoneMask;

        public event Action<Tower> TowerPlaced;

        public TowerPlacementService(PlayerWallet wallet, TowerFactory factory, LayerMask buildZoneMask)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _buildZoneMask = buildZoneMask;
        }

        /// <summary>
        /// Where a tower dropped at worldPosition would stand: the zone's center when over a zone,
        /// otherwise the position itself (used by the drag preview).
        /// </summary>
        public Vector3 GetSnappedPosition(Vector2 worldPosition)
        {
            BuildZone zone = FindZone(worldPosition);
            return zone != null ? zone.PlacementPosition : (Vector3)worldPosition;
        }

        public PlacementResult Validate(TowerConfig config, Vector2 worldPosition)
        {
            return Validate(config, worldPosition, out _);
        }

        public PlacementResult TryPlace(TowerConfig config, Vector2 worldPosition)
        {
            PlacementResult result = Validate(config, worldPosition, out BuildZone zone);
            if (result != PlacementResult.Success) return result;

            // Validate already checked gold, but TrySpend is the authoritative check.
            if (!_wallet.TrySpend(config.Cost)) return PlacementResult.NotEnoughGold;

            Tower tower = _factory.Create(config, zone.PlacementPosition);
            zone.Occupy(tower);
            TowerPlaced?.Invoke(tower);

            return PlacementResult.Success;
        }

        private PlacementResult Validate(TowerConfig config, Vector2 worldPosition, out BuildZone zone)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            zone = FindZone(worldPosition);

            if (!_wallet.CanAfford(config.Cost))
                return PlacementResult.NotEnoughGold;

            if (zone == null)
                return PlacementResult.OutsideBuildZone;

            if (zone.IsOccupied)
                return PlacementResult.ZoneOccupied;

            return PlacementResult.Success;
        }

        private BuildZone FindZone(Vector2 worldPosition)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPosition, _buildZoneMask);
            return hit != null ? hit.GetComponentInParent<BuildZone>() : null;
        }
    }
}
