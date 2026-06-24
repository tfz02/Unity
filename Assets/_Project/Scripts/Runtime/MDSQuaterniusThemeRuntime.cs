using System.Collections.Generic;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSQuaterniusThemeRuntime : MonoBehaviour
    {
        private readonly HashSet<GameObject> themedObjects = new();
        private MDSAssetCatalog catalog;
        private bool environmentCreated;
        private bool baseModelCreated;
        private bool spawnModelCreated;
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
                if (obj == null || themedObjects.Contains(obj))
                {
                    continue;
                }

                string n = obj.name;
                if (n.Contains("Cannon Tower") || n.Contains("Rapid Tower") || n.Contains("Sniper Tower") || n.Contains("Frost Tower"))
                {
                    GameObject prefab = PickTowerPrefab(n);
                    if (prefab != null)
                    {
                        ReplaceVisual(obj, prefab, new Vector3(0f, 0.05f, 0f), 0.85f, true, true);
                    }
                }
                else if (n.Contains("Enemy Drone"))
                {
                    if (catalog.opponentSmall != null)
                    {
                        ReplaceVisual(obj, catalog.opponentSmall, new Vector3(0f, 0.08f, 0f), 0.55f, true, true);
                    }
                }
                else if (n.Contains("Boss Drone"))
                {
                    if (catalog.opponentLarge != null)
                    {
                        ReplaceVisual(obj, catalog.opponentLarge, new Vector3(0f, 0.08f, 0f), 0.95f, true, true);
                    }
                }
                else if ((n == "Base Platform" || n == "Base Core") && !baseModelCreated)
                {
                    if (catalog.homeBase != null)
                    {
                        CreateSingleWorldModel("MDS_Western_Base_Model", catalog.homeBase, new Vector3(0f, 0.02f, -5.25f), Quaternion.Euler(0f, 180f, 0f), 2.05f);
                        baseModelCreated = true;
                    }
                }
                else if (n == "Spawn Platform" && !spawnModelCreated)
                {
                    if (catalog.startGate != null)
                    {
                        CreateSingleWorldModel("MDS_Western_Spawn_Model", catalog.startGate, new Vector3(0f, 0.02f, 5.2f), Quaternion.identity, 1.15f);
                        spawnModelCreated = true;
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

        private void ReplaceVisual(GameObject root, GameObject prefab, Vector3 localOffset, float targetMaxSize, bool hideOriginal, bool keepHpBars)
        {
            themedObjects.Add(root);

            if (root.transform.Find("MDS_ModelOverride") != null)
            {
                return;
            }

            if (hideOriginal)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (keepHpBars && renderer.gameObject.name.Contains("HP"))
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
            model.transform.localScale = Vector3.one;
            DisableModelColliders(model);
            NormalizeModel(model, targetMaxSize);
        }

        private void CreateWesternEnvironment()
        {
            PlaceArray(catalog.nature, new[]
            {
                new Vector3(-4.45f, 0.02f, 4.55f), new Vector3(4.45f, 0.02f, 4.45f),
                new Vector3(-4.65f, 0.02f, 1.8f), new Vector3(4.65f, 0.02f, 1.15f),
                new Vector3(-4.5f, 0.02f, -1.45f), new Vector3(4.55f, 0.02f, -1.9f),
                new Vector3(-4.35f, 0.02f, -4.25f), new Vector3(4.35f, 0.02f, -4.15f)
            }, 0.75f);

            PlaceArray(catalog.buildings, new[]
            {
                new Vector3(-5.65f, 0.02f, 3.75f), new Vector3(5.65f, 0.02f, 3.55f),
                new Vector3(-5.75f, 0.02f, -3.25f), new Vector3(5.75f, 0.02f, -3.45f)
            }, 1.25f);

            PlaceArray(catalog.trainProps, new[]
            {
                new Vector3(-3.7f, 0.04f, -5.25f), new Vector3(3.7f, 0.04f, -5.25f)
            }, 0.95f);
        }

        private void PlaceArray(GameObject[] prefabs, Vector3[] positions, float targetMaxSize)
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
                model.transform.localScale = Vector3.one;
                DisableModelColliders(model);
                NormalizeModel(model, targetMaxSize);
            }
        }

        private void CreateSingleWorldModel(string name, GameObject prefab, Vector3 position, Quaternion rotation, float targetMaxSize)
        {
            GameObject model = Instantiate(prefab);
            model.name = name;
            model.transform.position = position;
            model.transform.rotation = rotation;
            model.transform.localScale = Vector3.one;
            DisableModelColliders(model);
            NormalizeModel(model, targetMaxSize);
        }

        private void NormalizeModel(GameObject model, float targetMaxSize)
        {
            Bounds bounds = CalculateBounds(model);
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize <= 0.001f)
            {
                model.transform.localScale = Vector3.one * targetMaxSize;
                return;
            }

            float factor = targetMaxSize / maxSize;
            model.transform.localScale *= factor;

            Bounds scaledBounds = CalculateBounds(model);
            Vector3 offset = model.transform.position - scaledBounds.center;
            offset.y += scaledBounds.extents.y;
            model.transform.position += offset;
        }

        private Bounds CalculateBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(model.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
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
