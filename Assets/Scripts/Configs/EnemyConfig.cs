using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Tower Defense/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0.01f)] private float _moveSpeed = 2f;
        [SerializeField, Min(0)] private int _goldReward = 10;
        [SerializeField, Min(0), Tooltip("Lives the player loses when this enemy reaches the finish.")]
        private int _livesDamage = 1;
        [SerializeField] private Sprite _sprite;

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public int GoldReward => _goldReward;
        public int LivesDamage => _livesDamage;
        public Sprite Sprite => _sprite;
    }
}
