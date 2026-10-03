using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tower Defense/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _startingGold = 150;
        [SerializeField, Min(1)] private int _startingLives = 10;
        [SerializeField, Min(0.1f), Tooltip("Size of one tower placement cell in world units.")]
        private float _gridCellSize = 1f;

        public int StartingGold => _startingGold;
        public int StartingLives => _startingLives;
        public float GridCellSize => _gridCellSize;
    }
}
