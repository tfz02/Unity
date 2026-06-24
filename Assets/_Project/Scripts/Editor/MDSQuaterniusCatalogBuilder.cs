#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MergeDefenseSurvivor.Runtime;
using UnityEditor;
using UnityEngine;

namespace MergeDefenseSurvivor.EditorTools
{
    public static class MDSQuaterniusCatalogBuilder
    {
        private const string Root = "Assets/_Project/Art/External/Quaternius";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string CatalogPath = "Assets/_Project/Resources/MDSAssetCatalog.asset";

        [MenuItem("Tools/Merge Defense Survivor/Build Quaternius Catalog")]
        public static void BuildCatalog()
        {
            EnsureFolder("Assets/_Project", "Resources");

            MDSAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<MDSAssetCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MDSAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<GameObject> towerModels = FindModels("SteampunkTurretPack");
            List<GameObject> farmModels = FindModels("FarmBuildingsPack");
            List<GameObject> natureModels = FindModels("Nature Crops Pack").Concat(FindModels("Ultimate Nature Pack")).ToList();
            List<GameObject> trainModels = FindModels("Train Pack");
            List<GameObject> characterModels = FindModels("Ultimate Animated Character Pack");

            catalog.towerA = Pick(towerModels, "cannon", "turret", "gun", "tower") ?? towerModels.ElementAtOrDefault(0);
            catalog.towerB = Pick(towerModels, "rapid", "gatling", "machine", "small") ?? towerModels.ElementAtOrDefault(1) ?? catalog.towerA;
            catalog.towerC = Pick(towerModels, "sniper", "long", "rifle", "large") ?? towerModels.ElementAtOrDefault(2) ?? catalog.towerA;
            catalog.towerD = Pick(towerModels, "frost", "ice", "cryo", "blue") ?? towerModels.ElementAtOrDefault(3) ?? catalog.towerB ?? catalog.towerA;

            catalog.opponentSmall = Pick(characterModels, "cowboy", "bandit", "zombie", "goblin", "male") ?? characterModels.ElementAtOrDefault(0);
            catalog.opponentLarge = Pick(characterModels, "boss", "brute", "giant", "knight", "large") ?? characterModels.ElementAtOrDefault(1) ?? catalog.opponentSmall;

            catalog.homeBase = Pick(farmModels, "barn", "house", "mill", "silo") ?? farmModels.ElementAtOrDefault(0);
            catalog.startGate = Pick(trainModels, "station", "gate", "water", "tower") ?? Pick(farmModels, "well", "wind", "sign") ?? catalog.homeBase;

            catalog.buildings = PickMany(farmModels, 10, "barn", "house", "mill", "silo", "stable", "well", "fence");
            catalog.nature = PickMany(natureModels, 14, "cactus", "rock", "bush", "tree", "grass", "dead", "log", "crop");
            catalog.trainProps = PickMany(trainModels, 10, "rail", "track", "train", "wagon", "cart", "barrel", "crate");

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("MDS Quaternius catalog built. Towers: " + towerModels.Count + ", Farm: " + farmModels.Count + ", Nature: " + natureModels.Count + ", Train: " + trainModels.Count + ", Characters: " + characterModels.Count + ".");
        }

        private static List<GameObject> FindModels(string folderName)
        {
            string folder = Path.Combine(Root, folderName).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new List<GameObject>();
            }

            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => IsUsableModelPath(path))
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(go => go != null)
                .Distinct()
                .OrderBy(go => go.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsUsableModelPath(string path)
        {
            string lower = path.ToLowerInvariant();
            if (lower.Contains("/blend/") || lower.EndsWith(".blend")) return false;
            if (lower.Contains("/obj/") && !lower.EndsWith(".obj")) return false;
            return lower.EndsWith(".fbx") || lower.EndsWith(".obj") || lower.EndsWith(".prefab");
        }

        private static GameObject Pick(List<GameObject> models, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                GameObject hit = models.FirstOrDefault(model => model.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject[] PickMany(List<GameObject> models, int max, params string[] keywords)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (string keyword in keywords)
            {
                foreach (GameObject model in models)
                {
                    if (result.Contains(model)) continue;
                    if (model.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(model);
                    }
                    if (result.Count >= max) return result.ToArray();
                }
            }

            foreach (GameObject model in models)
            {
                if (result.Contains(model)) continue;
                result.Add(model);
                if (result.Count >= max) break;
            }

            return result.ToArray();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
