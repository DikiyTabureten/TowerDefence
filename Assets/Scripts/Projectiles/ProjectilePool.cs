using UnityEngine;

namespace TowerDefense
{
    /// <summary>Scene-level pool for projectiles. The only place projectiles are ever instantiated.</summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 30;

        private ObjectPool<Projectile> _pool;

        public void Initialize()
        {
            if (_pool != null) return;
            _pool = new ObjectPool<Projectile>(CreateProjectile, _prewarmCount);
        }

        public Projectile Rent()
        {
            Initialize();
            return _pool.Rent();
        }

        public void Return(Projectile projectile)
        {
            _pool.Return(projectile);
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(_prefab, transform);
            projectile.BindPool(this);
            return projectile;
        }
    }
}
