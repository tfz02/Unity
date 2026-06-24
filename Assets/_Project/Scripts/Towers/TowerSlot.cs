using MergeDefenseSurvivor.Economy;
using UnityEngine;

namespace MergeDefenseSurvivor.Towers
{
    public sealed class TowerSlot : MonoBehaviour
    {
        [SerializeField] private Tower towerPrefab;
        [SerializeField] private CurrencyWallet wallet;

        public Tower CurrentTower { get; private set; }
        public bool IsEmpty => CurrentTower == null;

        public bool TryBuyTower(TowerData towerData)
        {
            if (!IsEmpty || towerPrefab == null || towerData == null || wallet == null)
            {
                return false;
            }

            if (!wallet.TrySpend(towerData.BuyCost))
            {
                return false;
            }

            CurrentTower = Instantiate(towerPrefab, transform.position, Quaternion.identity, transform);
            CurrentTower.SetData(towerData);
            return true;
        }

        public void ReplaceTower(TowerData towerData)
        {
            if (CurrentTower == null || towerData == null)
            {
                return;
            }

            CurrentTower.SetData(towerData);
        }

        public void ClearTower()
        {
            if (CurrentTower == null)
            {
                return;
            }

            Destroy(CurrentTower.gameObject);
            CurrentTower = null;
        }
    }
}
