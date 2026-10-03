using UnityEngine;

namespace TowerDefense
{
    /// <summary>Shared data for towers that apply an effect to everything inside a radius.</summary>
    public abstract class AuraTowerConfig : TowerConfig
    {
        [SerializeField, Min(0.1f)] private float _effectRadius = 2.5f;
        [SerializeField, Min(0.02f), Tooltip("How often (seconds) the aura re-checks what is inside its radius.")]
        private float _scanInterval = 0.2f;

        public float EffectRadius => _effectRadius;
        public float ScanInterval => _scanInterval;
    }
}
