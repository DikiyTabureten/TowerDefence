using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Tower Defense/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Serializable]
        public sealed class SpawnGroup
        {
            [SerializeField] private EnemyConfig _enemy;
            [SerializeField, Min(1)] private int _count = 5;
            [SerializeField, Min(0f)] private float _spawnInterval = 0.8f;

            public EnemyConfig Enemy => _enemy;
            public int Count => _count;
            public float SpawnInterval => _spawnInterval;
        }

        [SerializeField, Min(0f)] private float _delayBeforeWave = 3f;
        [SerializeField] private List<SpawnGroup> _groups = new List<SpawnGroup>();

        public float DelayBeforeWave => _delayBeforeWave;
        public IReadOnlyList<SpawnGroup> Groups => _groups;
    }
}
