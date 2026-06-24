using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    [CreateAssetMenu(fileName = "MDSAssetCatalog", menuName = "Merge Defense Survivor/Asset Catalog")]
    public sealed class MDSAssetCatalog : ScriptableObject
    {
        [Header("Tower Models")]
        public GameObject towerA;
        public GameObject towerB;
        public GameObject towerC;
        public GameObject towerD;

        [Header("Opponent Models")]
        public GameObject opponentSmall;
        public GameObject opponentLarge;

        [Header("Western Buildings")]
        public GameObject homeBase;
        public GameObject startGate;

        [Header("Environment")]
        public GameObject[] buildings;
        public GameObject[] nature;
        public GameObject[] trainProps;

        public static MDSAssetCatalog LoadRuntimeCatalog()
        {
            return Resources.Load<MDSAssetCatalog>("MDSAssetCatalog");
        }
    }
}
