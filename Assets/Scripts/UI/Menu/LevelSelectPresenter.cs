using System;
using System.Linq;
using TowerDefense.Levels;

namespace TowerDefense
{
    /// <summary>Presenter of the main menu: fills the view from the LevelCatalog and starts the clicked level.</summary>
    public sealed class LevelSelectPresenter : IDisposable
    {
        private readonly LevelCatalog _catalog;
        private readonly LevelSelectView _view;
        private readonly SceneNavigator _navigator;

        public LevelSelectPresenter(LevelCatalog catalog, LevelSelectView view, SceneNavigator navigator)
        {
            _catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
            _view = view ? view : throw new ArgumentNullException(nameof(view));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void Initialize()
        {
            _view.LevelClicked += OnLevelClicked;
            _view.Show(_catalog.Levels
                .Select((level, i) => ($"{i + 1}. {level.DisplayName}", level.Description))
                .ToList());
        }

        public void Dispose()
        {
            if (_view != null) _view.LevelClicked -= OnLevelClicked;
        }

        private void OnLevelClicked(int index) => _navigator.LoadLevel(index);
    }
}
