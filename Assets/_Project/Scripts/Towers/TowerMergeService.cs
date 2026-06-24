using UnityEngine;

namespace MergeDefenseSurvivor.Towers
{
    public sealed class TowerMergeService : MonoBehaviour
    {
        public bool CanMerge(TowerSlot firstSlot, TowerSlot secondSlot)
        {
            if (firstSlot == null || secondSlot == null || firstSlot == secondSlot)
            {
                return false;
            }

            Tower firstTower = firstSlot.CurrentTower;
            Tower secondTower = secondSlot.CurrentTower;

            if (firstTower == null || secondTower == null)
            {
                return false;
            }

            TowerData firstData = firstTower.Data;
            TowerData secondData = secondTower.Data;

            return firstData != null
                && secondData != null
                && firstData == secondData
                && firstData.MergeResult != null;
        }

        public bool TryMerge(TowerSlot targetSlot, TowerSlot consumedSlot)
        {
            if (!CanMerge(targetSlot, consumedSlot))
            {
                return false;
            }

            TowerData mergeResult = targetSlot.CurrentTower.Data.MergeResult;
            targetSlot.ReplaceTower(mergeResult);
            consumedSlot.ClearTower();
            return true;
        }
    }
}
