using System.Collections;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Visual-only: turns the cannon's turret toward the tower's target and plays
    /// muzzle flash, recoil and smoke whenever ShooterTower fires. Holds no gameplay logic.
    /// </summary>
    [RequireComponent(typeof(ShooterTower))]
    public sealed class CannonAnimator : MonoBehaviour
    {
        [Header("Aiming")]
        [SerializeField] private Transform _turret;             // rotating child sprite
        [SerializeField, Min(0f), Tooltip("Degrees per second. 0 = snap instantly.")]
        private float _turnSpeed = 720f;
        [SerializeField, Range(-180f, 180f), Tooltip("Where the barrel points in the image: 0 = right.")]
        private float _spriteFacingAngle = 0f;

        [Header("Recoil")]
        [SerializeField] private float _recoilDistance = 0.09f;  // units the carriage kicks back
        [SerializeField] private float _recoilReturnTime = 0.25f;
        [SerializeField] private float _squash = 0.08f;          // 0 = no squash & stretch

        [Header("Muzzle flash")]
        [SerializeField] private SpriteRenderer _flash;         // child of turret, at the muzzle
        [SerializeField] private Sprite[] _flashFrames;         // cannon_flash_1, cannon_flash_2
        [SerializeField] private float _flashFrameTime = 0.04f;

        [Header("Smoke")]
        [SerializeField] private ParticleSystem _smoke;         // child of turret, at the muzzle
        [SerializeField] private int _smokeCount = 6;

        private ShooterTower _tower;
        private Vector3 _turretRestPos;
        private Coroutine _routine;

        private void Awake()
        {
            _tower = GetComponent<ShooterTower>();
            _turretRestPos = _turret.localPosition;
            if (_flash != null) _flash.enabled = false;
        }

        private void OnEnable() => _tower.Fired += OnFired;
        private void OnDisable() => _tower.Fired -= OnFired;

        private void LateUpdate()
        {
            Enemy target = _tower.CurrentTarget;
            if (target == null) return;

            float targetAngle = AngleTo(target.transform.position);
            float angle = _turnSpeed <= 0f
                ? targetAngle
                : Mathf.MoveTowardsAngle(_turret.eulerAngles.z, targetAngle, _turnSpeed * Time.deltaTime);
            _turret.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnFired(Enemy target)
        {
            // Make sure the barrel points at the target at the moment of the shot.
            if (target != null)
                _turret.rotation = Quaternion.Euler(0f, 0f, AngleTo(target.transform.position));

            PlayFire();
        }

        public void PlayFire()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FireRoutine());

            if (_smoke != null) _smoke.Emit(_smokeCount);
        }

        private float AngleTo(Vector3 worldPoint)
        {
            Vector2 dir = worldPoint - _turret.position;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - _spriteFacingAngle;
        }

        private IEnumerator FireRoutine()
        {
            if (_flash != null)
            {
                _flash.enabled = true;
                _flash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
                _flash.transform.localScale = Vector3.one * Random.Range(0.9f, 1.15f);
            }

            float elapsed = 0f;
            int frame = -1;
            while (elapsed < _recoilReturnTime)
            {
                if (_flash != null && _flashFrames != null)
                {
                    int f = Mathf.FloorToInt(elapsed / _flashFrameTime);
                    if (f != frame)
                    {
                        frame = f;
                        if (frame < _flashFrames.Length) _flash.sprite = _flashFrames[frame];
                        else _flash.enabled = false;
                    }
                }

                // Recoil: instant kick back along the barrel, smooth ease forward.
                float t = 1f - Mathf.SmoothStep(0f, 1f, elapsed / _recoilReturnTime);
                Vector3 back = -_turret.right * (_recoilDistance * t);
                _turret.localPosition = _turretRestPos + _turret.parent.InverseTransformVector(back);
                _turret.localScale = new Vector3(1f - _squash * t, 1f + _squash * t, 1f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_flash != null) _flash.enabled = false;
            _turret.localPosition = _turretRestPos;
            _turret.localScale = Vector3.one;
            _routine = null;
        }
    }
}
