namespace TowerDefense
{
    /// <summary>Something whose movement speed can be reduced by an external source (e.g. SlowAuraTower).</summary>
    public interface ISlowable
    {
        /// <summary>Applies (or updates) a speed multiplier from the given source. 0.5 = moves twice as slowly.</summary>
        void AddSpeedModifier(object source, float multiplier);

        /// <summary>Removes the multiplier previously applied by the given source.</summary>
        void RemoveSpeedModifier(object source);
    }
}
