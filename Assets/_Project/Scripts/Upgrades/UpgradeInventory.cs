using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Upgrades
{
    public sealed class UpgradeInventory : MonoBehaviour
    {
        private readonly List<UpgradeData> selectedUpgrades = new();

        public IReadOnlyList<UpgradeData> SelectedUpgrades => selectedUpgrades;

        public void AddUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            selectedUpgrades.Add(upgrade);
            Debug.Log($"Selected upgrade: {upgrade.DisplayName}");
        }

        public float GetTotalValue(UpgradeType type)
        {
            float total = 0f;

            foreach (UpgradeData upgrade in selectedUpgrades)
            {
                if (upgrade.Type == type)
                {
                    total += upgrade.Value;
                }
            }

            return total;
        }
    }
}
