using System;
using System.Collections;
using System.Collections.Generic;
using TowerDefense.Levels;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Spawns the waves of a level. Inside a wave every spawn group runs in parallel
    /// (own delay, own interval, own road); the next wave starts after the pause
    /// once the current wave has finished spawning. Knows nothing about LevelConfig assets:
    /// it is given the paths and the wave data by Bootstrap.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        private EnemyFactory _factory;
        private IReadOnlyList<WaypointPath> _paths;
        private Coroutine _routine;
        private readonly List<Coroutine> _groupRoutines = new List<Coroutine>();

        public event Action<Enemy> EnemySpawned;
        public event Action<int, int> WaveStarted; // wave number (1-based), total waves
        public event Action AllWavesSpawned;

        public void Init(EnemyFactory factory, IReadOnlyList<WaypointPath> paths)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if (paths == null || paths.Count == 0) throw new ArgumentException("At least one path is required.", nameof(paths));
            _paths = paths;
        }

        public void StartWaves(IReadOnlyList<LevelConfig.WaveData> waves, float timeBetweenWaves)
        {
            if (_factory == null)
                throw new InvalidOperationException("EnemySpawner.Init must be called before StartWaves.");

            if (_routine == null)
                _routine = StartCoroutine(RunWaves(waves, timeBetweenWaves));
        }

        public void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            foreach (Coroutine group in _groupRoutines)
                if (group != null) StopCoroutine(group);

            _routine = null;
            _groupRoutines.Clear();
        }

        private IEnumerator RunWaves(IReadOnlyList<LevelConfig.WaveData> waves, float timeBetweenWaves)
        {
            for (int i = 0; i < waves.Count; i++)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
                WaveStarted?.Invoke(i + 1, waves.Count);

                int running = 0;
                _groupRoutines.Clear();
                foreach (LevelConfig.SpawnGroup group in waves[i].groups)
                {
                    running++;
                    _groupRoutines.Add(StartCoroutine(RunGroup(group, () => running--)));
                }

                // The wave is over when every group has spawned its last enemy.
                while (running > 0)
                    yield return null;
            }

            _routine = null;
            _groupRoutines.Clear();
            AllWavesSpawned?.Invoke();
        }

        private IEnumerator RunGroup(LevelConfig.SpawnGroup group, Action onFinished)
        {
            if (group.delayBeforeGroup > 0f)
                yield return new WaitForSeconds(group.delayBeforeGroup);

            WaypointPath path = _paths[Mathf.Clamp(group.pathIndex, 0, _paths.Count - 1)];
            for (int n = 0; n < group.count; n++)
            {
                if (group.enemy != null) Spawn(group.enemy, path);
                if (n < group.count - 1)
                    yield return new WaitForSeconds(group.spawnInterval);
            }

            onFinished();
        }

        private void Spawn(EnemyConfig config, WaypointPath path)
        {
            Enemy enemy = _factory.Create(config, path);
            EnemySpawned?.Invoke(enemy);
        }
    }
}
