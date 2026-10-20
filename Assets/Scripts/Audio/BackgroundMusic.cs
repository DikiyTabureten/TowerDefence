using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Observer of tower placement: the background music starts (and loops) as soon as
    /// the first War Orchestra is placed on the map. Plain class, created by Bootstrap.
    /// </summary>
    public sealed class BackgroundMusic : IDisposable
    {
        private readonly TowerPlacementService _placement;
        private readonly AudioSource _source;

        public BackgroundMusic(TowerPlacementService placement, AudioSource source)
        {
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
            _source = source;
            _placement.TowerPlaced += OnTowerPlaced;
        }

        public void Dispose() => _placement.TowerPlaced -= OnTowerPlaced;

        private void OnTowerPlaced(Tower tower)
        {
            if (!(tower is AttackSpeedAuraTower)) return;

            _placement.TowerPlaced -= OnTowerPlaced; // only the first orchestra matters
            if (_source != null && !_source.isPlaying)
            {
                _source.loop = true;
                _source.Play();
            }
        }
    }
}
