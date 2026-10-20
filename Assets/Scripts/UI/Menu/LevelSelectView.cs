using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// View of the main menu: builds one button per level and reports which one was clicked.
    /// Knows nothing about LevelConfig or scenes — the presenter gives it plain texts.
    /// </summary>
    public sealed class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private LevelButtonView _buttonPrefab;
        [SerializeField] private Transform _buttonsContainer;

        private readonly List<LevelButtonView> _buttons = new List<LevelButtonView>();

        public event Action<int> LevelClicked;

        public void Show(IReadOnlyList<(string title, string description)> levels)
        {
            Clear();
            for (int i = 0; i < levels.Count; i++)
            {
                LevelButtonView button = Instantiate(_buttonPrefab, _buttonsContainer);
                button.Setup(levels[i].title, levels[i].description);
                button.Clicked += OnButtonClicked;
                _buttons.Add(button);
            }
        }

        private void OnButtonClicked(LevelButtonView button) => LevelClicked?.Invoke(_buttons.IndexOf(button));

        private void Clear()
        {
            foreach (LevelButtonView button in _buttons)
            {
                if (button == null) continue;
                button.Clicked -= OnButtonClicked;
                Destroy(button.gameObject);
            }
            _buttons.Clear();
        }

        private void OnDestroy() => Clear();
    }
}
