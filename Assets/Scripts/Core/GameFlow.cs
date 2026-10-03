using System;

namespace TowerDefense
{
    /// <summary>Decides when the game is won or lost. Contains no UI and no spawning logic.</summary>
    public sealed class GameFlow : IDisposable
    {
        private readonly EnemySpawner _spawner;
        private readonly EnemyOutcomeHandler _enemies;
        private readonly PlayerLives _lives;

        private bool _allWavesSpawned;
        private bool _isFinished;

        public event Action Victory;
        public event Action Defeat;

        public GameFlow(EnemySpawner spawner, EnemyOutcomeHandler enemies, PlayerLives lives)
        {
            _spawner = spawner;
            _enemies = enemies;
            _lives = lives;

            _spawner.AllWavesSpawned += OnAllWavesSpawned;
            _enemies.EnemyRemoved += CheckVictory;
            _lives.Depleted += OnLivesDepleted;
        }

        public void Dispose()
        {
            if (_spawner != null) _spawner.AllWavesSpawned -= OnAllWavesSpawned;
            _enemies.EnemyRemoved -= CheckVictory;
            _lives.Depleted -= OnLivesDepleted;
        }

        private void OnAllWavesSpawned()
        {
            _allWavesSpawned = true;
            CheckVictory();
        }

        private void CheckVictory()
        {
            if (_isFinished || !_allWavesSpawned || _enemies.ActiveEnemies > 0) return;

            _isFinished = true;
            Victory?.Invoke();
        }

        private void OnLivesDepleted()
        {
            if (_isFinished) return;

            _isFinished = true;
            Defeat?.Invoke();
        }
    }
}
