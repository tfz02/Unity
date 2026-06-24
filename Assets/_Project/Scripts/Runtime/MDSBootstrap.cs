using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public static class MDSBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGameIfMissing()
        {
            if (UnityEngine.Object.FindAnyObjectByType<MDSModern3DGame>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Modern3D_Runtime");
            root.AddComponent<MDSModern3DGame>();
        }
    }
}
