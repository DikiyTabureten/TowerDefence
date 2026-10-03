using System;
using System.Collections.Generic;

namespace TowerDefense
{
    /// <summary>
    /// Keeps one multiplier per source and exposes their product.
    /// Keying by source means overlapping auras never overwrite each other,
    /// and removing one aura can never "undo" another aura's effect.
    /// </summary>
    public sealed class ModifierStack
    {
        private readonly Dictionary<object, float> _modifiers = new Dictionary<object, float>();

        public float Value { get; private set; } = 1f;

        public event Action<float> Changed;

        public void Set(object source, float multiplier)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            _modifiers[source] = multiplier;
            Recalculate();
        }

        public void Remove(object source)
        {
            if (source != null && _modifiers.Remove(source))
                Recalculate();
        }

        public void Clear()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            float value = 1f;
            foreach (float multiplier in _modifiers.Values)
                value *= multiplier;

            Value = value;
            Changed?.Invoke(Value);
        }
    }
}
