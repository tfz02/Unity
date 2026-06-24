using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSVisualTuning : MonoBehaviour
    {
        private bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {
            if (Object.FindAnyObjectByType<MDSVisualTuning>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Visual_Tuning");
            root.AddComponent<MDSVisualTuning>();
        }

        private void LateUpdate()
        {
            if (applied)
            {
                return;
            }

            if (Object.FindAnyObjectByType<MDSModern3DGame>() == null)
            {
                return;
            }

            ApplyCameraAndLighting();
            ApplyObjectColors();
            applied = true;
        }

        private static void ApplyCameraAndLighting()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.45f, 0.56f, 0.65f);
                camera.orthographic = true;
                camera.orthographicSize = 6.85f;
            }

            RenderSettings.ambientLight = new Color(0.34f, 0.37f, 0.40f);

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type != LightType.Directional)
                {
                    continue;
                }

                if (light.name.Contains("Key"))
                {
                    light.intensity = 0.72f;
                    light.color = new Color(1.0f, 0.94f, 0.84f);
                }
                else if (light.name.Contains("Fill"))
                {
                    light.intensity = 0.20f;
                    light.color = new Color(0.68f, 0.78f, 0.92f);
                }
                else
                {
                    light.intensity = 0.42f;
                }
            }
        }

        private static void ApplyObjectColors()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                string objectName = renderer.gameObject.name;

                if (objectName.Contains("Ground"))
                {
                    SetColor(renderer, new Color(0.30f, 0.42f, 0.32f));
                }
                else if (objectName.Contains("Road") || objectName.Contains("Center Mark"))
                {
                    SetColor(renderer, new Color(0.36f, 0.37f, 0.34f));
                }
                else if (objectName.Contains("Spawn"))
                {
                    SetColor(renderer, new Color(0.22f, 0.55f, 0.30f));
                }
                else if (objectName.Contains("Base Platform") || objectName.Contains("Base Tower"))
                {
                    SetColor(renderer, new Color(0.30f, 0.40f, 0.52f));
                }
                else if (objectName.Contains("Base Core"))
                {
                    SetColor(renderer, new Color(0.78f, 0.22f, 0.18f));
                }
                else if (objectName.Contains("Build Pad"))
                {
                    SetColor(renderer, new Color(0.24f, 0.27f, 0.30f));
                }
            }
        }

        private static void SetColor(Renderer renderer, Color color)
        {
            Material material = renderer.material;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
