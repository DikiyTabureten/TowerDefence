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
        [SerializeField, Tooltip("All levels in order. The menu's choice is an index into this list.")]
        private LevelCatalog _catalog;
        [SerializeField, Tooltip("Played when the Game scene is started directly (no choice from the menu).")]
        private LevelConfig _defaultLevel;
        [SerializeField] private List<TowerConfig> _shopTowers = new List<TowerConfig>();

        [Header("Level")]
        [SerializeField] private MapBuilder _mapBuilder;
        [SerializeField] private BuildZone _buildZonePrefab;
        [SerializeField] private Transform _buildZonesRoot;
        [SerializeField] private Transform _pathsRoot;

        [Header("Scene")]
        [SerializeField] private Camera _gameCamera;
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
        [SerializeField] private SoundToggleView _soundToggleView;

        [Header("Audio")]
        [SerializeField, Tooltip("Plays the level's rhythm track; shooter towers fire on its notes.")]
        private BeatConductor _beatConductor;
        [SerializeField, Tooltip("Plays every cannon shot as a note of the melody.")]
        private ShotNotePlayer _shotNotePlayer;
        [SerializeField, Tooltip("Fades the music in while at least one War Orchestra is on the map.")]
        private BackgroundMusic _backgroundMusic;

        private TowerShopPresenter _shopPresenter;
        private SoundTogglePresenter _soundTogglePresenter;
        private GameFlow _gameFlow;
        private EnemyOutcomeHandler _enemyOutcomes;
        private ResultPresenter _resultPresenter;
        private LevelConfig _level;
        private LoadedLevel _loadedLevel;

        private void Awake()
        {
            Time.timeScale = 1f;

            // Which level: the menu's choice, or the default one when the scene is started directly
            _level = LevelSelection.HasSelection && _catalog != null
                ? _catalog.Get(LevelSelection.SelectedIndex)
                : null;
            if (_level == null) _level = _defaultLevel;
            int levelIndex = _catalog != null ? _catalog.IndexOf(_level) : -1;

            // Level: map, roads, build zones and camera from LevelConfig
            var levelLoader = new LevelLoader(_mapBuilder, _buildZonePrefab, _buildZonesRoot, _pathsRoot, _gameCamera);
            _loadedLevel = levelLoader.Load(_level);

            // Economy
            var wallet = new PlayerWallet(_level.StartingGold);
            var lives = new PlayerLives(_level.StartingLives);

            // Towers & placement
            _projectilePool.Initialize();
            IShotNoteSink shotSounds = _shotNotePlayer != null ? _shotNotePlayer : null; // real null if unassigned
            var towerContext = new TowerContext(_projectilePool, _enemyMask, _towerMask, _beatConductor, shotSounds);
            var towerFactory = new TowerFactory(towerContext, _towersRoot);
            var placementService = new TowerPlacementService(wallet, towerFactory, _buildZoneMask);

            // Shop (MVP)
            var shopModel = new TowerShopModel(_shopTowers);
            _shopPresenter = new TowerShopPresenter(shopModel, _shopView, placementService, wallet, _gameCamera);
            _shopPresenter.Initialize();

            // Enemies
            _enemyOutcomes = new EnemyOutcomeHandler(wallet, lives);
            _spawner.Init(new EnemyFactory(_enemyPrefab, _enemiesRoot), _loadedLevel.Paths);
            _spawner.EnemySpawned += _enemyOutcomes.Register;
            _spawner.WaveStarted += _hudView.ShowWave;

            // Win / lose
            _gameFlow = new GameFlow(_spawner, _enemyOutcomes, lives);
            _gameFlow.Defeat += OnDefeat;

            // HUD and result panel (MVP: next level / restart / menu)
            _hudView.Bind(wallet, lives);
            _hudView.ShowWave(0, _level.Waves.Count); // "0/5" until the first wave starts
            _resultPresenter = new ResultPresenter(_hudView, _gameFlow, _catalog, levelIndex, new SceneNavigator());
            _resultPresenter.Initialize();

            // Audio: music while an orchestra is on the map, sound on/off switch (MVP)
            if (_backgroundMusic != null) _backgroundMusic.Init(_towersRoot);
            if (_soundToggleView != null)
            {
                _soundTogglePresenter = new SoundTogglePresenter(new SoundSettings(), _soundToggleView);
                _soundTogglePresenter.Initialize();
            }
        }

        private void Start()
        {
            _spawner.StartWaves(_level.Waves, _level.TimeBetweenWaves);
        }

        private void OnDefeat()
        {
            _spawner.Stop();
            Time.timeScale = 0f;
            if (_beatConductor != null) _beatConductor.Pause(); // the audio clock ignores timeScale
        }

        private void OnDestroy()
        {
            _shopPresenter?.Dispose();
            _resultPresenter?.Dispose();
            _soundTogglePresenter?.Dispose();

            if (_gameFlow != null)
            {
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
