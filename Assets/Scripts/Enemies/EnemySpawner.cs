using System;
using System.Collections;
using UnityEngine;

namespace TowerDefense
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private WaveConfig[] _waves;

        private EnemyFactory _factory;
        private WaypointPath _path;
        private Coroutine _routine;

        public event Action<Enemy> EnemySpawned;
        public event Action<int, int> WaveStarted; // wave number (1-based), total waves
        public event Action AllWavesSpawned;

        public void Init(EnemyFactory factory, WaypointPath path)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _path = path ? path : throw new ArgumentNullException(nameof(path));
        }

        public void StartWaves()
        {
            if (_factory == null)
                throw new InvalidOperationException("EnemySpawner.Init must be called before StartWaves.");

            if (_routine == null)
                _routine = StartCoroutine(RunWaves());
        }

        public void Stop()
        {
            if (_routine == null) return;

            StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < _waves.Length; i++)
            {
                WaveConfig wave = _waves[i];
                yield return new WaitForSeconds(wave.DelayBeforeWave);

                WaveStarted?.Invoke(i + 1, _waves.Length);

                foreach (WaveConfig.SpawnGroup group in wave.Groups)
                {
                    for (int n = 0; n < group.Count; n++)
                    {
                        Spawn(group.Enemy);
                        yield return new WaitForSeconds(group.SpawnInterval);
                    }
                }
            }

            _routine = null;
            AllWavesSpawned?.Invoke();
        }

        private void Spawn(EnemyConfig config)
        {
            Enemy enemy = _factory.Create(config, _path);
            EnemySpawned?.Invoke(enemy);
        }
    }
}
