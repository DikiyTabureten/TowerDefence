using UnityEngine;

namespace TowerDefense
{
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f), Tooltip("Distance at which the projectile counts as hitting its target.")]
        private float _hitDistance = 0.15f;

        private ProjectilePool _pool;
        private Enemy _target;
        private float _damage;
        private float _speed;
        private bool _isFlying;

        public void BindPool(ProjectilePool pool) => _pool = pool;

        public void Launch(Enemy target, float damage, float speed)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _isFlying = true;
        }

        private void Update()
        {
            if (!_isFlying) return;

            // Target was killed by someone else or reached the finish: go back to the pool.
            if (_target == null || !_target.IsAlive)
            {
                ReturnToPool();
                return;
            }

            Vector3 position = transform.position;
            Vector3 toTarget = _target.transform.position - position;
            float step = _speed * Time.deltaTime;
            float hitDistance = Mathf.Max(step, _hitDistance);

            if (toTarget.sqrMagnitude <= hitDistance * hitDistance)
            {
                _target.TakeDamage(_damage);
                ReturnToPool();
                return;
            }

            transform.position = position + toTarget.normalized * step;

            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ReturnToPool()
        {
            _isFlying = false;

            if (_pool != null)
                _pool.Return(this);
            else
                gameObject.SetActive(false);
        }

        // IPoolable
        public void OnRent() { }

        public void OnReturn()
        {
            _target = null;
            _isFlying = false;
        }
    }
}
