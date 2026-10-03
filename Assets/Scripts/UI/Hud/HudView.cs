using TMPro;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>Displays gold, lives, wave number and the end-of-game banner. Observes the models via events.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _livesText;
        [SerializeField] private TMP_Text _waveText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private string _victoryMessage = "Victory!";
        [SerializeField] private string _defeatMessage = "Defeat";

        private PlayerWallet _wallet;
        private PlayerLives _lives;

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

        public void ShowVictory() => ShowResult(_victoryMessage);
        public void ShowDefeat() => ShowResult(_defeatMessage);

        private void ShowResult(string message)
        {
            if (_resultText != null) _resultText.text = message;
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
