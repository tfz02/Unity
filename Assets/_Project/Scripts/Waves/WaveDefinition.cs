using UnityEngine;

namespace MergeDefenseSurvivor.Waves
{
    [CreateAssetMenu(menuName = "MergeDefenseSurvivor/Waves/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int EnemyCount { get; private set; } = 10;
        [field: SerializeField, Min(0.05f)] public float SpawnInterval { get; private set; } = 0.75f;
        [field: SerializeField, Min(0f)] public float StartDelay { get; private set; } = 1f;
    }
}
