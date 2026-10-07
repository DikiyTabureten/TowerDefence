using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Connects TowerShopView to the game: converts screen positions to world positions
    /// and forwards commands to TowerPlacementService. No physics, no tower creation.
    /// </summary>
    public sealed class TowerShopPresenter : IDisposable
    {
        private readonly TowerShopModel _model;
        private readonly TowerShopView _view;
        private readonly TowerPlacementService _placementService;
        private readonly PlayerWallet _wallet;
        private readonly Camera _camera;

        public TowerShopPresenter(
            TowerShopModel model,
            TowerShopView view,
            TowerPlacementService placementService,
            PlayerWallet wallet,
            Camera camera)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ? view : throw new ArgumentNullException(nameof(view));
            _placementService = placementService ?? throw new ArgumentNullException(nameof(placementService));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _camera = camera ? camera : throw new ArgumentNullException(nameof(camera));
        }

        public void Initialize()
        {
            _view.Show(_model.Towers);

            _view.DragStarted += OnDragStarted;
            _view.Dragged += OnDragged;
            _view.DragEnded += OnDragEnded;
            _wallet.GoldChanged += OnGoldChanged;

            OnGoldChanged(_wallet.Gold);
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.DragStarted -= OnDragStarted;
                _view.Dragged -= OnDragged;
                _view.DragEnded -= OnDragEnded;
            }

            _wallet.GoldChanged -= OnGoldChanged;
        }

        private void OnGoldChanged(int gold)
        {
            for (int i = 0; i < _model.Towers.Count; i++)
                _view.SetCardAffordable(i, _wallet.CanAfford(_model.Towers[i].Cost));
        }

        private void OnDragStarted(TowerConfig config, Vector2 screenPosition)
        {
            _view.ShowDragPreview(config.Icon);
            RefreshPreview(config, screenPosition);
        }

        private void OnDragged(TowerConfig config, Vector2 screenPosition)
        {
            RefreshPreview(config, screenPosition);
        }

        private void OnDragEnded(TowerConfig config, Vector2 screenPosition)
        {
            _view.HideDragPreview();

            // Dropped back onto the shop: treat as cancel.
            if (_view.IsPointerOverShop(screenPosition)) return;

            PlacementResult result = _placementService.TryPlace(config, ScreenToWorld(screenPosition));
            if (result != PlacementResult.Success)
                _view.ShowMessage(ToMessage(result));
        }

        private void RefreshPreview(TowerConfig config, Vector2 screenPosition)
        {
            if (_view.IsPointerOverShop(screenPosition))
            {
                _view.UpdateDragPreview(screenPosition, false);
                return;
            }

            Vector2 world = ScreenToWorld(screenPosition);
            bool isValid = _placementService.Validate(config, world) == PlacementResult.Success;

            // Show the preview where the tower would actually snap to.
            Vector2 snappedScreen = _camera.WorldToScreenPoint(_placementService.GetSnappedPosition(world));
            _view.UpdateDragPreview(snappedScreen, isValid);
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return _camera.ScreenToWorldPoint(screenPosition);
        }

        private static string ToMessage(PlacementResult result)
        {
            switch (result)
            {
                case PlacementResult.NotEnoughGold: return "Not enough gold";
                case PlacementResult.OutsideBuildZone: return "Can't build here";
                case PlacementResult.ZoneOccupied: return "This spot is taken";
                default: return string.Empty;
            }
        }
    }
}
