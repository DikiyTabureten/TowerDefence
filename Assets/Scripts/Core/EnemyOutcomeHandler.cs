using System;

namespace TowerDefense
{
    /// <summary>
    /// Observer that reacts to Enemy.Died / Enemy.ReachedFinish:
    /// pays gold rewards and takes lives. Also counts enemies still on the field.
    /// </summary>
    public sealed class EnemyOutcomeHandler
    {
        private readonly PlayerWallet _wallet;
        private readonly PlayerLives _lives;

        public int ActiveEnemies { get; private set; }

        public event Action EnemyRemoved;

        public EnemyOutcomeHandler(PlayerWallet wallet, PlayerLives lives)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _lives = lives ?? throw new ArgumentNullException(nameof(lives));
        }

        public void Register(Enemy enemy)
        {
            ActiveEnemies++;
            enemy.Died += OnEnemyDied;
            enemy.ReachedFinish += OnEnemyReachedFinish;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            Unregister(enemy);
            _wallet.Add(enemy.Config.GoldReward);
            EnemyRemoved?.Invoke();
        }

        private void OnEnemyReachedFinish(Enemy enemy)
        {
            Unregister(enemy);
            _lives.Lose(enemy.Config.LivesDamage);
            EnemyRemoved?.Invoke();
        }

        private void Unregister(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;
            enemy.ReachedFinish -= OnEnemyReachedFinish;
            ActiveEnemies--;
        }
    }
}
