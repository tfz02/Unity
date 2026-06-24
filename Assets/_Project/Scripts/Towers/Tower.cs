using MergeDefenseSurvivor.Enemies;
using UnityEngine;

namespace MergeDefenseSurvivor.Towers
{
    public sealed class Tower : MonoBehaviour
    {
        [SerializeField] private TowerData data;
        [SerializeField] private LayerMask enemyLayerMask;

        private float fireTimer;

        public TowerData Data => data;
        public bool HasData => data != null;

        private void Update()
        {
            if (data == null)
            {
                return;
            }

            fireTimer -= Time.deltaTime;

            if (fireTimer > 0f)
            {
                return;
            }

            Enemy target = FindClosestEnemy();

            if (target == null)
            {
                return;
            }

            target.TakeDamage(data.Damage);
            fireTimer = 1f / data.FireRate;
        }

        public void SetData(TowerData newData)
        {
            data = newData;
            fireTimer = 0f;
        }

        private Enemy FindClosestEnemy()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.Range, enemyLayerMask);
            Enemy closest = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent(out Enemy enemy) || !enemy.IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, enemy.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null)
            {
                return;
            }

            Gizmos.DrawWireSphere(transform.position, data.Range);
        }
    }
}
