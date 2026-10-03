namespace TowerDefense
{
    /// <summary>An object that can be reused by ObjectPool instead of being instantiated/destroyed.</summary>
    public interface IPoolable
    {
        /// <summary>Called right after the object is taken from the pool and activated.</summary>
        void OnRent();

        /// <summary>Called right before the object is deactivated and put back into the pool. Reset state here.</summary>
        void OnReturn();
    }
}
