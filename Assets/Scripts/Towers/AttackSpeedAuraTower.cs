using UnityEngine;

namespace TowerDefense
{
    public sealed class AttackSpeedAuraTower : AuraTower<IAttackSpeedModifiable>
    {
        private AttackSpeedTowerConfig _config;

        protected override LayerMask GetTargetMask(TowerContext context) => context.TowerMask;

        protected override void OnAuraInit(TowerConfig config)
        {
            _config = RequireConfig<AttackSpeedTowerConfig>(config);
        }

        protected override void ApplyEffect(IAttackSpeedModifiable target)
        {
            target.AddAttackSpeedModifier(this, _config.AttackSpeedMultiplier);
        }

        protected override void RemoveEffect(IAttackSpeedModifiable target)
        {
            target.RemoveAttackSpeedModifier(this);
        }
    }
}
