namespace TowerDefense
{
    /// <summary>Something whose attack rate can be changed by an external source (e.g. AttackSpeedAuraTower).</summary>
    public interface IAttackSpeedModifiable
    {
        /// <summary>Applies (or updates) an attack speed multiplier from the given source. 1.5 = fires 50% faster.</summary>
        void AddAttackSpeedModifier(object source, float multiplier);

        /// <summary>Removes the multiplier previously applied by the given source.</summary>
        void RemoveAttackSpeedModifier(object source);
    }
}
