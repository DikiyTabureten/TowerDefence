using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    public enum EnemyFacingMode
    {
        /// <summary>Rotate the whole sprite towards the next waypoint (top-down art).</summary>
        RotateTowardsWaypoint,
        /// <summary>Only mirror the sprite left/right (side-view art).</summary>
        FlipHorizontally,
        /// <summary>Never change the sprite's orientation.</summary>
        None
    }

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

        [Header("Facing")]
        [SerializeField] private EnemyFacingMode _facingMode = EnemyFacingMode.RotateTowardsWaypoint;
        [SerializeField, Range(-180f, 180f),
         Tooltip("Which way the character looks in the image: 0 = right, 90 = up, -90 = down, 180 = left.")]
        private float _spriteFacingAngle = 0f;
        [SerializeField, Min(0f), Tooltip("Turn speed in degrees per second. 0 = turn instantly.")]
        private float _turnSpeed = 540f;

        [Header("Animation (visual only)")]
        [SerializeField, Tooltip("Walk cycle frames in order. Empty = the static Sprite is used.")]
        private List<Sprite> _walkFrames = new List<Sprite>();
        [SerializeField, Min(0.05f), Tooltip("World units travelled during one full walk cycle.")]
        private float _distancePerCycle = 0.5f;
        [SerializeField, Tooltip("Kick up dust while moving (e.g. the horseman).")]
        private bool _emitsDust;

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public int GoldReward => _goldReward;
        public int LivesDamage => _livesDamage;
        public Sprite Sprite => _sprite;
        public EnemyFacingMode FacingMode => _facingMode;
        public float SpriteFacingAngle => _spriteFacingAngle;
        public float TurnSpeed => _turnSpeed;
        public IReadOnlyList<Sprite> WalkFrames => _walkFrames;
        public float DistancePerCycle => _distancePerCycle;
        public bool EmitsDust => _emitsDust;
    }
}
