using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tower Defense/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _startingGold = 150;
        [SerializeField, Min(1)] private int _startingLives = 10;

        public int StartingGold => _startingGold;
        public int StartingLives => _startingLives;
    }
}
