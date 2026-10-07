using System;
using UnityEngine;

namespace TowerDefense
{
    public enum RangeDisplayMode
    {
        Never,
        /// <summary>In the Editor and in Development Builds only.</summary>
        DebugOnly,
        Always
    }

    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public abstract class Tower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Range display")]
        [SerializeField] private RangeDisplayMode _rangeDisplay = RangeDisplayMode.DebugOnly;
        [SerializeField] private Color _rangeColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField, Min(0.005f)] private float _rangeLineWidth = 0.04f;
        [SerializeField] private int _rangeSortingOrder = 5;
        [SerializeField, Tooltip("Optional. If empty, the built-in Sprites/Default material is used.")]
        private Material _rangeMaterial;

        private RangeIndicator _rangeIndicator;

        public TowerConfig Config { get; private set; }
        public bool IsInitialized => Config != null;

        /// <summary>Attack radius for shooters, effect radius for auras.</summary>
        public abstract float Radius { get; }

        public void Init(TowerConfig config, TowerContext context)
        {
            Config = config ? config : throw new ArgumentNullException(nameof(config));

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (config.Sprite != null)
                _spriteRenderer.sprite = config.Sprite;

            OnInit(config, context);
            CreateRangeIndicator();
        }

        public void SetRangeVisible(bool isVisible)
        {
            if (_rangeIndicator != null)
                _rangeIndicator.SetVisible(isVisible);
        }

        protected abstract void OnInit(TowerConfig config, TowerContext context);

        protected static T RequireConfig<T>(TowerConfig config) where T : TowerConfig
        {
            if (config is T typed) return typed;

            throw new ArgumentException(
                $"Config '{config.name}' is {config.GetType().Name}, but this tower expects {typeof(T).Name}. " +
                "Check that the config's prefab matches its tower type.");
        }

        protected static bool IsWithinRadius(Vector3 center, Vector3 point, float radius)
        {
            return ((Vector2)(point - center)).sqrMagnitude <= radius * radius;
        }

        private void CreateRangeIndicator()
        {
            bool show = _rangeDisplay == RangeDisplayMode.Always
                || (_rangeDisplay == RangeDisplayMode.DebugOnly && Debug.isDebugBuild);
            if (!show) return;

            _rangeIndicator = RangeIndicator.Create(
                transform, Radius, _rangeColor, _rangeLineWidth, _rangeMaterial, _rangeSortingOrder);
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsInitialized) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
