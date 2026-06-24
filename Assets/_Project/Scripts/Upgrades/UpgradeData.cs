using UnityEngine;

namespace MergeDefenseSurvivor.Upgrades
{
    public enum UpgradeType
    {
        TowerDamagePercent,
        TowerFireRatePercent,
        StartCoinsFlat,
        BaseHealthFlat
    }

    [CreateAssetMenu(menuName = "MergeDefenseSurvivor/Upgrades/Upgrade Data", fileName = "UpgradeData")]
    public sealed class UpgradeData : ScriptableObject
    {
        [field: SerializeField] public string UpgradeId { get; private set; } = "upgrade_basic";
        [field: SerializeField] public string DisplayName { get; private set; } = "Upgrade";
        [field: SerializeField, TextArea] public string Description { get; private set; } = "Upgrade description";
        [field: SerializeField] public UpgradeType Type { get; private set; }
        [field: SerializeField] public float Value { get; private set; } = 1f;
    }
}
