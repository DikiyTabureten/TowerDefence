using System;
using UnityEngine;

namespace TowerDefense
{
    public sealed class PlayerLives
    {
        public int Current { get; private set; }
        public bool IsDepleted => Current <= 0;

        public event Action<int> Changed;
        public event Action Depleted;

        public PlayerLives(int startingLives)
        {
            Current = Mathf.Max(1, startingLives);
        }

        public void Lose(int amount)
        {
            if (amount <= 0 || IsDepleted) return;

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current);

            if (IsDepleted)
                Depleted?.Invoke();
        }
    }
}
