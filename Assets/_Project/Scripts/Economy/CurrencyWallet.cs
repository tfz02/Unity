using System;
using UnityEngine;

namespace MergeDefenseSurvivor.Economy
{
    public sealed class CurrencyWallet : MonoBehaviour
    {
        [SerializeField] private int startCoins = 100;

        public int Coins { get; private set; }

        public event Action<int> CoinsChanged;

        private void Awake()
        {
            Coins = Mathf.Max(0, startCoins);
            CoinsChanged?.Invoke(Coins);
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && Coins >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }
    }
}
