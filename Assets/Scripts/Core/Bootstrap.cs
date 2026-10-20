using System.Collections.Generic;
using TowerDefense.Levels;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Composition root: creates the systems and wires them together, then gets out of the way.
    /// It holds references, not game logic — that is what separates it from a "GameManager".
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private List<TowerConfig> _shopTowers = new List<TowerConfig>();
        [SerializeField, Tooltip("Map, enemy path and build zones are built from this asset.")]
        private LevelConfig _level;

        [Header("Level")]
        [SerializeField] private MapBuilder _mapBuilder;
        [SerializeField] private BuildZone _buildZonePrefab;
        [SerializeField] private Transform _buildZonesRoot;

        [Header("Scene")]
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private WaypointPath _path;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private ProjectilePool _projectilePool;
        [SerializeField] private Transform _towersRoot;
        [SerializeField] private Transform _enemiesRoot;

        [Header("Prefabs")]
        [SerializeField] private Enemy _enemyPrefab;

        [Header("Physics layers")]
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _towerMask;
        [SerializeField] private LayerMask _buildZoneMask;

        [Header("UI")]
        [SerializeField] private TowerShopView _shopView;
        [SerializeField] private HudView _hudView;

        private TowerShopPresenter _shopPresenter;
        private GameFlow _gameFlow;
        private EnemyOutcomeHandler _enemyOutcomes;

        private void Awake()
        {
            Time.timeScale = 1f;

            // Level: map, enemy path and build zones from LevelConfig
            new LevelLoader(_mapBuilder, _path, _buildZonePrefab, _buildZonesRoot).Load(_level);

            // Economy
            var wallet = new PlayerWallet(_gameConfig.StartingGold);
            var lives = new PlayerLives(_gameConfig.StartingLives);

            // Towers & placement
            _projectilePool.Initialize();
            var towerContext = new TowerContext(_projectilePool, _enemyMask, _towerMask);
            var towerFactory = new TowerFactory(towerContext, _towersRoot);
            var placementService = new TowerPlacementService(wallet, towerFactory, _buildZoneMask);

            // Shop (MVP)
            var shopModel = new TowerShopModel(_shopTowers);
            _shopPresenter = new TowerShopPresenter(shopModel, _shopView, placementService, wallet, _gameCamera);
            _shopPresenter.Initialize();

            // Enemies
            _enemyOutcomes = new EnemyOutcomeHandler(wallet, lives);
            _spawner.Init(new EnemyFactory(_enemyPrefab, _enemiesRoot), _path);
            _spawner.EnemySpawned += _enemyOutcomes.Register;
            _spawner.WaveStarted += _hudView.ShowWave;

            // Win / lose
            _gameFlow = new GameFlow(_spawner, _enemyOutcomes, lives);
            _gameFlow.Victory += OnVictory;
            _gameFlow.Defeat += OnDefeat;

            _hudView.Bind(wallet, lives);
        }

        private void Start()
        {
            _spawner.StartWaves();
        }

        private void OnVictory()
        {
            _hudView.ShowVictory();
        }

        private void OnDefeat()
        {
            _spawner.Stop();
            _hudView.ShowDefeat();
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            _shopPresenter?.Dispose();

            if (_gameFlow != null)
            {
                _gameFlow.Victory -= OnVictory;
                _gameFlow.Defeat -= OnDefeat;
                _gameFlow.Dispose();
            }

            if (_spawner != null && _enemyOutcomes != null)
            {
                _spawner.EnemySpawned -= _enemyOutcomes.Register;
                _spawner.WaveStarted -= _hudView.ShowWave;
            }
        }
    }
}
