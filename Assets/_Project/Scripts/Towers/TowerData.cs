using UnityEngine;

namespace MergeDefenseSurvivor.Towers
{
    [CreateAssetMenu(menuName = "MergeDefenseSurvivor/Towers/Tower Data", fileName = "TowerData")]
    public sealed class TowerData : ScriptableObject
    {
        [field: SerializeField] public string TowerId { get; private set; } = "basic";
        [field: SerializeField] public string DisplayName { get; private set; } = "Basic Tower";
        [field: SerializeField, Min(1)] public int Level { get; private set; } = 1;
        [field: SerializeField, Min(1)] public int Damage { get; private set; } = 1;
        [field: SerializeField, Min(0.1f)] public float Range { get; private set; } = 3f;
        [field: SerializeField, Min(0.05f)] public float FireRate { get; private set; } = 1f;
        [field: SerializeField, Min(1)] public int BuyCost { get; private set; } = 25;
        [field: SerializeField] public TowerData MergeResult { get; private set; }
    }
}
