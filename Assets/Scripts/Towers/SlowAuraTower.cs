using UnityEngine;

namespace TowerDefense
{
    public sealed class SlowAuraTower : AuraTower<ISlowable>
    {
        private SlowTowerConfig _config;

        protected override LayerMask GetTargetMask(TowerContext context) => context.EnemyMask;

        protected override void OnAuraInit(TowerConfig config)
        {
            _config = RequireConfig<SlowTowerConfig>(config);
        }

        protected override void ApplyEffect(ISlowable target)
        {
            target.AddSpeedModifier(this, _config.SpeedMultiplier);
        }

        protected override void RemoveEffect(ISlowable target)
        {
            target.RemoveSpeedModifier(this);
        }
    }
}
