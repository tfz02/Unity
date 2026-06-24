using MergeDefenseSurvivor.Prototype;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public static class MDSBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGameIfMissing()
        {
            if (Object.FindFirstObjectByType<MDSPrototypeGame>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Game_Runtime");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<MDSPrototypeGame>();
        }
    }
}
