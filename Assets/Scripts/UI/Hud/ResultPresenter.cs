using System;
using TowerDefense.Levels;

namespace TowerDefense
{
    /// <summary>
    /// Presenter for the end-of-level panel: shows the result when GameFlow reports victory/defeat
    /// and turns the panel's buttons into scene changes (next level, restart, menu).
    /// </summary>
    public sealed class ResultPresenter : IDisposable
    {
        private readonly HudView _view;
        private readonly GameFlow _gameFlow;
        private readonly LevelCatalog _catalog;
        private readonly int _levelIndex;
        private readonly SceneNavigator _navigator;

        public ResultPresenter(HudView view, GameFlow gameFlow, LevelCatalog catalog, int levelIndex, SceneNavigator navigator)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
            _catalog = catalog;
            _levelIndex = levelIndex;
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void Initialize()
        {
            _gameFlow.Victory += OnVictory;
            _gameFlow.Defeat += OnDefeat;
            _view.NextLevelClicked += OnNextLevel;
            _view.RestartClicked += OnRestart;
            _view.MenuClicked += OnMenu;
        }

        public void Dispose()
        {
            _gameFlow.Victory -= OnVictory;
            _gameFlow.Defeat -= OnDefeat;
            if (_view == null) return; // already destroyed with the scene
            _view.NextLevelClicked -= OnNextLevel;
            _view.RestartClicked -= OnRestart;
            _view.MenuClicked -= OnMenu;
        }

        private bool HasNextLevel => _catalog != null && _catalog.HasNext(_levelIndex);

        private void OnVictory() => _view.ShowVictory(HasNextLevel);
        private void OnDefeat() => _view.ShowDefeat();

        private void OnNextLevel()
        {
            if (HasNextLevel) _navigator.LoadLevel(_levelIndex + 1);
        }

        private void OnRestart() => _navigator.LoadLevel(Math.Max(_levelIndex, 0));
        private void OnMenu() => _navigator.OpenMenu();
    }
}
