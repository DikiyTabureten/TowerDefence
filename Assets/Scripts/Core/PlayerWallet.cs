using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>The player's gold. Plain C# class: it knows nothing about UI or towers.</summary>
    public sealed class PlayerWallet
    {
        public int Gold { get; private set; }

        /// <summary>Raised with the new balance whenever gold changes.</summary>
        public event Action<int> GoldChanged;

        public PlayerWallet(int startingGold)
        {
            Gold = Mathf.Max(0, startingGold);
        }

        public bool CanAfford(int amount) => amount >= 0 && amount <= Gold;

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!CanAfford(amount)) return false;

            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }
    }
}
