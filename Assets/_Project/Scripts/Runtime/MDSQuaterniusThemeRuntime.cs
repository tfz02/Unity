using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSQuaterniusThemeRuntime : MonoBehaviour
    {
        private readonly HashSet<int> themedObjects = new();
        private MDSAssetCatalog catalog;
        private bool environmentCreated;
        private float scanTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {
            if (Object.FindAnyObjectByType<MDSQuaterniusThemeRuntime>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Quaternius_Theme_Runtime");
            root.AddComponent<MDSQuaterniusThemeRuntime>();
        }

        private void Start()
        {
            catalog = MDSAssetCatalog.LoadRuntimeCatalog();
        }

        private void Update()
        {
            if (catalog == null)
            {
                catalog = MDSAssetCatalog.LoadRuntimeCatalog();
                if (catalog == null)
                {
                    return;
                }
            }

            if (!environmentCreated)
            {
                CreateWesternEnvironment();
                environmentCreated = true;
            }

            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f)
            {
                return;
            }

            scanTimer = 0.25f;
            ApplyThemeToRuntimeObjects();
        }

        private void ApplyThemeToRuntimeObjects()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj == null || themedObjects.Contains(obj.GetInstanceID()))
                {
                    continue;
                }

                string n = obj.name;
                if (n.Contains("Cannon Tower") || n.Contains("Rapid Tower") || n.Contains("Sniper Tower") || n.Contains("Frost Tower"))
                {
                    GameObject prefab = PickTowerPrefab(n);
                    if (prefab != null)
                    {
                        ReplaceVisual(obj, prefab, new Vector3(0f, 0.06f, 0f), 0.55f, true);
                    }
                }
                else if (n.Contains("Enemy Drone"))
                {
                    if (catalog.opponentSmall != null)
                    {
                        ReplaceVisual(obj, catalog.opponentSmall, new Vector3(0f, 0.12f, 0f), 0.42f, true);
                    }
                }
                else if (n.Contains("Boss Drone"))
                {
                    if (catalog.opponentLarge != null)
                    {
                        ReplaceVisual(obj, catalog.opponentLarge, new Vector3(0f, 0.1f, 0f), 0.72f, true);
                    }
                }
                else if (n == "Base Platform" || n == "Base Core")
                {
                    if (catalog.homeBase != null)
                    {
                        ReplaceVisual(obj, catalog.homeBase, new Vector3(0f, 0.0f, -0.15f), 0.75f, false);
                    }
                }
                else if (n == "Spawn Platform")
                {
                    if (catalog.startGate != null)
                    {
                        ReplaceVisual(obj, catalog.startGate, new Vector3(0f, 0.0f, 0.0f), 0.58f, false);
                    }
                }
            }
        }

        private GameObject PickTowerPrefab(string runtimeName)
        {
            if (runtimeName.Contains("Cannon")) return catalog.towerA != null ? catalog.towerA : catalog.towerC;
            if (runtimeName.Contains("Rapid")) return catalog.towerB != null ? catalog.towerB : catalog.towerA;
            if (runtimeName.Contains("Sniper")) return catalog.towerC != null ? catalog.towerC : catalog.towerA;
            if (runtimeName.Contains("Frost")) return catalog.towerD != null ? catalog.towerD : catalog.towerB;
            return catalog.towerA;
        }

        private void ReplaceVisual(GameObject root, GameObject prefab, Vector3 localOffset, float scale, bool hideOriginal)
        {
            themedObjects.Add(root.GetInstanceID());

            if (root.transform.Find("MDS_ModelOverride") != null)
            {
                return;
            }

            if (hideOriginal)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name.Contains("HP"))
                    {
                        continue;
                    }

                    renderer.enabled = false;
                }
            }

            GameObject model = Instantiate(prefab, root.transform);
            model.name = "MDS_ModelOverride";
            model.transform.localPosition = localOffset;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * scale;
            DisableModelColliders(model);
        }

        private void CreateWesternEnvironment()
        {
            PlaceArray(catalog.nature, new[]
            {
                new Vector3(-4.1f, 0.02f, 4.2f), new Vector3(4.1f, 0.02f, 4.1f),
                new Vector3(-4.2f, 0.02f, 1.6f), new Vector3(4.25f, 0.02f, -0.9f),
                new Vector3(-4.0f, 0.02f, -3.7f), new Vector3(4.0f, 0.02f, -3.6f)
            }, 0.55f);

            PlaceArray(catalog.buildings, new[]
            {
                new Vector3(-5.2f, 0.02f, 2.9f), new Vector3(5.25f, 0.02f, 2.55f),
                new Vector3(-5.15f, 0.02f, -2.15f), new Vector3(5.2f, 0.02f, -2.55f)
            }, 0.62f);

            PlaceArray(catalog.trainProps, new[]
            {
                new Vector3(-3.95f, 0.04f, -4.85f), new Vector3(3.95f, 0.04f, -4.85f)
            }, 0.5f);
        }

        private void PlaceArray(GameObject[] prefabs, Vector3[] positions, float scale)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject prefab = prefabs[i % prefabs.Length];
                if (prefab == null)
                {
                    continue;
                }

                GameObject model = Instantiate(prefab);
                model.name = "MDS_Theme_Decor_" + prefab.name;
                model.transform.position = positions[i];
                model.transform.rotation = Quaternion.Euler(0f, (i * 73f) % 360f, 0f);
                model.transform.localScale = Vector3.one * scale;
                DisableModelColliders(model);
            }
        }

        private void DisableModelColliders(GameObject model)
        {
            Collider[] colliders = model.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
        }
    }
}
