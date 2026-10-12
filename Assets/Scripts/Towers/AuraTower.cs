using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Base for towers that apply an effect to every TTarget inside a radius.
    /// Each scan it compares "what is in range now" with "what was affected last time":
    /// newcomers get the effect, leavers lose it.
    /// </summary>
    public abstract class AuraTower<TTarget> : Tower, IAuraTower where TTarget : class
    {
        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private readonly HashSet<TTarget> _affected = new HashSet<TTarget>();
        private readonly HashSet<TTarget> _inRange = new HashSet<TTarget>();
        private readonly List<TTarget> _leaving = new List<TTarget>();

        private AuraTowerConfig _auraConfig;
        private ContactFilter2D _filter;
        private float _scanTimer;

        public override float Radius => _auraConfig != null ? _auraConfig.EffectRadius : 0f;

        // IAuraTower (used by visuals)
        public int AffectedCount => _affected.Count;
        public event Action<int> AffectedCountChanged;

        protected abstract LayerMask GetTargetMask(TowerContext context);
        protected abstract void OnAuraInit(TowerConfig config);
        protected abstract void ApplyEffect(TTarget target);
        protected abstract void RemoveEffect(TTarget target);

        protected sealed override void OnInit(TowerConfig config, TowerContext context)
        {
            _auraConfig = RequireConfig<AuraTowerConfig>(config);

            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(GetTargetMask(context));

            OnAuraInit(config);
            Scan();
        }

        private void Update()
        {
            if (_auraConfig == null) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;

            _scanTimer = _auraConfig.ScanInterval;
            Scan();
        }

        private void Scan()
        {
            int countBefore = _affected.Count;

            CollectTargetsInRange();

            _leaving.Clear();
            foreach (TTarget target in _affected)
            {
                if (!_inRange.Contains(target))
                    _leaving.Add(target);
            }

            foreach (TTarget target in _leaving)
            {
                _affected.Remove(target);
                if (IsAlive(target))
                    RemoveEffect(target);
            }

            foreach (TTarget target in _inRange)
            {
                if (_affected.Add(target))
                    ApplyEffect(target);
            }

            if (_affected.Count != countBefore)
                AffectedCountChanged?.Invoke(_affected.Count);
        }

        private void CollectTargetsInRange()
        {
            _inRange.Clear();
            _hits.Clear();
            Physics2D.OverlapCircle(transform.position, _auraConfig.EffectRadius, _filter, _hits);

            foreach (Collider2D hit in _hits)
            {
                TTarget target = hit.GetComponentInParent<TTarget>();
                if (target == null || ReferenceEquals(target, this)) continue;
                if (!IsWithinRadius(transform.position, hit.transform.position, _auraConfig.EffectRadius)) continue;

                _inRange.Add(target);
            }
        }

        private void OnDisable()
        {
            // If the aura tower is ever removed, don't leave its effect behind.
            foreach (TTarget target in _affected)
            {
                if (IsAlive(target))
                    RemoveEffect(target);
            }

            bool hadTargets = _affected.Count > 0;
            _affected.Clear();

            if (hadTargets)
                AffectedCountChanged?.Invoke(0);
        }

        /// <summary>Unity objects can be destroyed while we still hold a reference to them.</summary>
        private static bool IsAlive(TTarget target)
        {
            return !(target is UnityEngine.Object unityObject) || unityObject != null;
        }
    }
}
