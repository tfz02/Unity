#if UNITY_EDITOR
using MergeDefenseSurvivor.Runtime;
using UnityEditor;
using UnityEngine;

namespace MergeDefenseSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class MDSQuaterniusAutoCatalog
    {
        static MDSQuaterniusAutoCatalog()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            MDSAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<MDSAssetCatalog>("Assets/_Project/Resources/MDSAssetCatalog.asset");
            if (CatalogLooksReady(catalog))
            {
                return;
            }

            Debug.Log("MDS: Quaternius catalog missing or empty. Building catalog before Play.");
            MDSQuaterniusCatalogBuilder.BuildCatalog();
        }

        private static bool CatalogLooksReady(MDSAssetCatalog catalog)
        {
            if (catalog == null)
            {
                return false;
            }

            return catalog.towerA != null
                || catalog.towerB != null
                || catalog.towerC != null
                || catalog.towerD != null
                || catalog.homeBase != null
                || catalog.buildings != null && catalog.buildings.Length > 0
                || catalog.nature != null && catalog.nature.Length > 0
                || catalog.trainProps != null && catalog.trainProps.Length > 0;
        }
    }
}
#endif
