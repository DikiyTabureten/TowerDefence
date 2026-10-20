using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    /// <summary>
    /// Displays gold, lives, wave number and the end-of-game panel. Observes the models via events.
    /// The result panel's buttons only raise events — what they do is decided by ResultPresenter.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _livesText;
        [SerializeField] private TMP_Text _waveText;

        [Header("Result panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _victoryMessage = "Victory!";
        [SerializeField] private string _lastLevelMessage = "All levels completed!";
        [SerializeField] private string _defeatMessage = "Defeat";

        private PlayerWallet _wallet;
        private PlayerLives _lives;

        public event Action NextLevelClicked;
        public event Action RestartClicked;
        public event Action MenuClicked;

        private void Awake()
        {
            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(() => NextLevelClicked?.Invoke());
            if (_restartButton != null) _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            if (_menuButton != null) _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        public void Bind(PlayerWallet wallet, PlayerLives lives)
        {
            Unbind();

            _wallet = wallet;
            _lives = lives;

            _wallet.GoldChanged += OnGoldChanged;
            _lives.Changed += OnLivesChanged;

            OnGoldChanged(_wallet.Gold);
            OnLivesChanged(_lives.Current);

            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        public void ShowWave(int wave, int total)
        {
            if (_waveText != null) _waveText.text = $"Wave {wave}/{total}";
        }

        /// <summary>Victory: "Next level" (if there is one) and "Menu".</summary>
        public void ShowVictory(bool hasNextLevel) =>
            ShowResult(hasNextLevel ? _victoryMessage : _lastLevelMessage, next: hasNextLevel, restart: false);

        /// <summary>Defeat: "Restart" and "Menu".</summary>
        public void ShowDefeat() => ShowResult(_defeatMessage, next: false, restart: true);

        private void ShowResult(string message, bool next, bool restart)
        {
            if (_resultText != null) _resultText.text = message;
            if (_nextLevelButton != null) _nextLevelButton.gameObject.SetActive(next);
            if (_restartButton != null) _restartButton.gameObject.SetActive(restart);
            if (_menuButton != null) _menuButton.gameObject.SetActive(true);
            if (_resultPanel != null) _resultPanel.SetActive(true);
        }

        private void OnGoldChanged(int gold)
        {
            if (_goldText != null) _goldText.text = gold.ToString();
        }

        private void OnLivesChanged(int lives)
        {
            if (_livesText != null) _livesText.text = lives.ToString();
        }

        private void Unbind()
        {
            if (_wallet != null) _wallet.GoldChanged -= OnGoldChanged;
            if (_lives != null) _lives.Changed -= OnLivesChanged;
        }

        private void OnDestroy() => Unbind();
    }
}
