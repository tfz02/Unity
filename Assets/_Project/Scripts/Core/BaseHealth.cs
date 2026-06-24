using System;
using UnityEngine;

namespace MergeDefenseSurvivor.Core
{
    public sealed class BaseHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 20;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;

        public event Action<int, int> HealthChanged;
        public event Action BaseDestroyed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth == 0)
            {
                BaseDestroyed?.Invoke();
                GameManager.Instance?.TriggerGameOver();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
