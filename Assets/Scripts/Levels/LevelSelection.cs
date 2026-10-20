using UnityEngine;

namespace TowerDefense.Levels
{
    /// <summary>
    /// Carries the chosen level index from the main menu into the Game scene.
    /// A static field survives SceneManager.LoadScene, and it is reset at the start of every Play session,
    /// so pressing Play in the Game scene directly always starts from the default level.
    /// Holds one number and no logic — it is not a game manager.
    /// </summary>
    public static class LevelSelection
    {
        public const int None = -1;

        public static int SelectedIndex { get; private set; } = None;
        public static bool HasSelection => SelectedIndex != None;

        public static void Select(int index) => SelectedIndex = index;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => SelectedIndex = None;
    }
}
