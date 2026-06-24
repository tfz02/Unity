using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSCameraAutoFrame : MonoBehaviour
    {
        private const float CameraHeight = 9.2f;
        private const float CameraZ = -5.8f;
        private const float CameraTilt = 58f;
        private const float CameraSize = 6.85f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {
            if (Object.FindAnyObjectByType<MDSCameraAutoFrame>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Camera_AutoFrame");
            root.AddComponent<MDSCameraAutoFrame>();
        }

        private void LateUpdate()
        {
            if (Object.FindAnyObjectByType<MDSModern3DGame>() == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, CameraHeight, CameraZ);
            camera.transform.rotation = Quaternion.Euler(CameraTilt, 0f, 0f);
            camera.orthographicSize = CameraSize;
        }
    }
}
