using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Moves between the menu and the game. Every level change reloads the Game scene,
    /// so events, pools, wallet and towers start clean without any manual reset.
    /// </summary>
    public sealed class SceneNavigator
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public void LoadLevel(int index)
        {
            LevelSelection.Select(index);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScene);
        }

        public void OpenMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
