using System;
using UnityEngine;

namespace TowerDefense
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public abstract class Tower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public TowerConfig Config { get; private set; }
        public bool IsInitialized => Config != null;

        /// <summary>Radius shown in the Scene view when the tower is selected.</summary>
        protected abstract float GizmoRadius { get; }

        public void Init(TowerConfig config, TowerContext context)
        {
            Config = config ? config : throw new ArgumentNullException(nameof(config));

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (config.Sprite != null)
                _spriteRenderer.sprite = config.Sprite;

            OnInit(config, context);
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

        private void OnDrawGizmosSelected()
        {
            if (!IsInitialized) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, GizmoRadius);
        }
    }
}
