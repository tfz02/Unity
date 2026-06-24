using UnityEditor;
using UnityEngine;

namespace MergeDefenseSurvivor.EditorTools
{
    public static class MDSBuildSettingsHelper
    {
        [MenuItem("Tools/MergeDefenseSurvivor/Add Main Scene To Build")]
        public static void AddMainSceneToBuild()
        {
            const string scenePath = "Assets/_Project/Scenes/Main.unity";
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Main scene added to Build Settings.");
        }
    }
}
