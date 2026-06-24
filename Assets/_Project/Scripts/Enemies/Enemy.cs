using System;
using MergeDefenseSurvivor.Economy;
using UnityEngine;

namespace MergeDefenseSurvivor.Enemies
{
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int rewardCoins = 5;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private Transform target;
        [SerializeField] private CurrencyWallet wallet;

        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public event Action<Enemy> Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void Update()
        {
            if (!IsAlive || target == null)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime);
        }

        public void Initialize(Transform newTarget, CurrencyWallet newWallet)
        {
            target = newTarget;
            wallet = newWallet;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            if (CurrentHealth == 0)
            {
                Die();
            }
        }

        private void Die()
        {
            wallet?.Add(rewardCoins);
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
