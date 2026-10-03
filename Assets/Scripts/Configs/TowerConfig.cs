using UnityEngine;

namespace TowerDefense
{
    /// <summary>Data shared by every tower type. Concrete tower types extend this with their own parameters.</summary>
    public abstract class TowerConfig : ScriptableObject
    {
        [SerializeField] private string _displayName = "Tower";
        [SerializeField, Tooltip("Shown on the shop card.")] private Sprite _icon;
        [SerializeField, Tooltip("Shown on the game field.")] private Sprite _sprite;
        [SerializeField, Min(0)] private int _cost = 50;
        [SerializeField, Tooltip("Prefab whose Tower component matches this config type.")]
        private Tower _prefab;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Sprite Sprite => _sprite;
        public int Cost => _cost;
        public Tower Prefab => _prefab;
    }
}
