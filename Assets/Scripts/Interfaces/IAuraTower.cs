using System;

namespace TowerDefense
{
    /// <summary>
    /// Read-only view of an aura tower for visual components.
    /// Lets views react to the aura without knowing its target type (AuraTower&lt;T&gt; is generic).
    /// </summary>
    public interface IAuraTower
    {
        float Radius { get; }

        /// <summary>How many targets the aura currently affects.</summary>
        int AffectedCount { get; }

        /// <summary>Raised with the new count whenever targets enter or leave the aura.</summary>
        event Action<int> AffectedCountChanged;
    }
}
