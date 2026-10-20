using TowerDefense.Levels;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>Composition root of the MainMenu scene: wires the level list (View + Presenter) to the catalog.</summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelCatalog _catalog;
        [SerializeField] private LevelSelectView _view;

        private LevelSelectPresenter _presenter;

        private void Awake()
        {
            Time.timeScale = 1f;
            _presenter = new LevelSelectPresenter(_catalog, _view, new SceneNavigator());
            _presenter.Initialize();
        }

        private void OnDestroy() => _presenter?.Dispose();
    }
}
