using System;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Utilities;
using UnityEngine;

namespace MasterBuilderHeeler.Core.Camera
{
    public static class BuildCameraAutoPickup
    {
        private const float ScanInterval = 0.2f;
        private const float AlertDebounceInterval = 3.0f;

        private static readonly Collider[] NearbyColliders = new Collider[128];
        private static readonly int ItemLayerMask = LayerMask.GetMask("item");

        private static float _nextScanTime;
        private static float _nextAlertTime;

        public static void ScanAndCollect(Player player, Vector3 cameraPosition)
        {
            if (!ModConfig.EnableMod.Value ||
                !ModConfig.EnableAutoPickup.Value ||
                !Player.m_enableAutoPickup ||
                player.IsDead())
            {
                return;
            }

            if (Time.time < _nextScanTime)
            {
                return;
            }
            _nextScanTime = Time.time + ScanInterval;

            if (ModConfig.RequireComfortForAutoPickup.Value &&
                !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting))
            {
                return;
            }

            float radius = ModConfig.AutoPickupRadius.Value;
            int count = Physics.OverlapSphereNonAlloc(cameraPosition, radius, NearbyColliders, ItemLayerMask);

            for (int index = 0; index < count; index++)
            {
                ItemDrop itemDrop = NearbyColliders[index].GetComponentInParent<ItemDrop>();
                if (!itemDrop ||
                    !itemDrop.m_nview ||
                    !itemDrop.m_nview.IsValid() ||
                    !itemDrop.m_autoPickup ||
                    itemDrop.InTar())
                {
                    continue;
                }

                if (ModConfig.EnforcePrivateAreaCheck.Value && !PrivateArea.CheckAccess(itemDrop.transform.position, 0f, false))
                {
                    continue;
                }

                // load zdo stack data before the weight checks, and sort out ownership before Pickup destroys the drop
                itemDrop.Load();

                if (!player.m_inventory.CanAddItem(itemDrop.m_itemData, itemDrop.m_itemData.m_stack))
                {
                    ShowDebouncedAlert("$msg_inventoryfull");
                    continue;
                }

                float totalWeight = player.m_inventory.GetTotalWeight();
                float itemWeight = itemDrop.m_itemData.GetWeight();
                float maxWeight = player.GetMaxCarryWeight();

                if (totalWeight + itemWeight > maxWeight)
                {
                    ShowDebouncedAlert("$se_encumbered_start");
                    continue;
                }

                if (!itemDrop.CanPickup(false))
                {
                    itemDrop.RequestOwn();
                    continue;
                }

                player.Pickup(itemDrop.gameObject, false, false);
            }

            Array.Clear(NearbyColliders, 0, count);
        }

        private static void ShowDebouncedAlert(string messageKey)
        {
            if (Time.time < _nextAlertTime)
            {
                return;
            }

            _nextAlertTime = Time.time + AlertDebounceInterval;
            HudMessageUtils.ShowTopLeft(messageKey);
        }
    }
}
