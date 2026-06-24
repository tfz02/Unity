using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MergeDefenseSurvivor.Runtime
{
    public sealed class MDSTowerLaneFitPatch : MonoBehaviour
    {
        private const float TargetSideX = 1.72f;
        private const float MinimumOldSideX = 2.1f;

        private FieldInfo slotsField;
        private FieldInfo slotPositionField;
        private FieldInfo slotPadField;
        private FieldInfo slotTowerField;
        private FieldInfo towerRootField;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {
            if (Object.FindAnyObjectByType<MDSTowerLaneFitPatch>() != null)
            {
                return;
            }

            GameObject root = new GameObject("MDS_Tower_Lane_Fit_Patch");
            root.AddComponent<MDSTowerLaneFitPatch>();
        }

        private void LateUpdate()
        {
            MDSModern3DGame game = Object.FindAnyObjectByType<MDSModern3DGame>();
            if (game == null)
            {
                return;
            }

            EnsureReflection(game);
            if (slotsField == null || slotPositionField == null)
            {
                return;
            }

            if (slotsField.GetValue(game) is not IEnumerable slots)
            {
                return;
            }

            foreach (object slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                Vector3 pos = (Vector3)slotPositionField.GetValue(slot);
                if (Mathf.Abs(pos.x) < MinimumOldSideX)
                {
                    continue;
                }

                pos.x = Mathf.Sign(pos.x) * TargetSideX;
                slotPositionField.SetValue(slot, pos);

                GameObject pad = slotPadField?.GetValue(slot) as GameObject;
                if (pad != null)
                {
                    Vector3 padPos = pad.transform.position;
                    pad.transform.position = new Vector3(pos.x, padPos.y, pos.z);
                }

                object tower = slotTowerField?.GetValue(slot);
                if (tower != null && towerRootField != null)
                {
                    GameObject towerRoot = towerRootField.GetValue(tower) as GameObject;
                    if (towerRoot != null)
                    {
                        Vector3 towerPos = towerRoot.transform.position;
                        towerRoot.transform.position = new Vector3(pos.x, towerPos.y, pos.z);
                    }
                }
            }
        }

        private void EnsureReflection(MDSModern3DGame game)
        {
            if (slotsField != null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type gameType = game.GetType();
            slotsField = gameType.GetField("slots", flags);

            object slotsObject = slotsField?.GetValue(game);
            if (slotsObject is not IEnumerable slots)
            {
                return;
            }

            foreach (object slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                System.Type slotType = slot.GetType();
                slotPositionField = slotType.GetField("Position", flags | BindingFlags.Public);
                slotPadField = slotType.GetField("Pad", flags | BindingFlags.Public);
                slotTowerField = slotType.GetField("Tower", flags | BindingFlags.Public);
                break;
            }

            if (slotTowerField == null)
            {
                return;
            }

            foreach (object slot in slots)
            {
                object tower = slotTowerField.GetValue(slot);
                if (tower == null)
                {
                    continue;
                }

                towerRootField = tower.GetType().GetField("Root", flags | BindingFlags.Public);
                break;
            }
        }
    }
}
