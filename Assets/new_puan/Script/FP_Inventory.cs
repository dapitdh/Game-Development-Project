using System.Collections.Generic;
using UnityEngine;

namespace FPP
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemData data;
        public int count = 1;
    }

    public class FP_Inventory : MonoBehaviour
    {
        [Header("Refs")]
        public Transform itemHolderSocket;   // drag: ItemHolder (child CameraParent)
        public Transform playerBody;         // drag: puan (root)
        public Camera mainCam;             // drag: MainCamera (Base)
        public string fpLayerName = "FP_Arms";

        [Header("Drop")]
        public float dropForward = 0.8f;
        public float dropUp = 0.4f;
        public float dropThrowForce = 2f;

        [Header("Slots")]
        public List<ItemStack> slots = new List<ItemStack>(); // simple list
        public int equippedIndex = -1;

        [Header("Runtime")]
        public ItemData equippedData;        // read-only
        public GameObject equippedInstance;  // read-only

        public bool HasItem => equippedInstance != null;

        // ====== PUBLIC API ======
        public void Pickup(ItemWorld world)
        {
            if (world == null || world.data == null) return;

            Add(world.data, world.quantity <= 0 ? 1 : world.quantity);
            Destroy(world.gameObject);

            // auto-equip kalau belum pegang apa-apa
            if (equippedIndex < 0)
                EquipByData(world.data);
        }

        public void Add(ItemData data, int amount = 1)
        {
            if (data == null || amount <= 0) return;

            // cari stack yang sama
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].data == data)
                {
                    slots[i].count += amount;
                    return;
                }
            }
            // buat stack baru
            slots.Add(new ItemStack { data = data, count = amount });
        }

        public void EquipByIndex(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            EquipByData(slots[index].data);
            equippedIndex = index;
        }

        public void EquipByData(ItemData data)
        {
            if (data == null || data.fpPrefab == null || itemHolderSocket == null) return;

            // cari index stack
            int idx = slots.FindIndex(s => s.data == data && s.count > 0);
            if (idx < 0) return;

            // destroy viewmodel lama
            if (equippedInstance) Destroy(equippedInstance);

            // spawn viewmodel di tangan
            equippedInstance = Instantiate(data.fpPrefab, itemHolderSocket);
            equippedInstance.transform.localPosition = Vector3.zero;
            equippedInstance.transform.localRotation = Quaternion.identity;
            equippedInstance.transform.localScale = Vector3.one;

            // pastikan layer dan nonaktifkan fisika (viewmodel)
            int fpLayer = LayerMask.NameToLayer(fpLayerName);
            if (fpLayer >= 0) SetLayerRecursively(equippedInstance, fpLayer);
            foreach (var c in equippedInstance.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var r in equippedInstance.GetComponentsInChildren<Rigidbody>()) r.isKinematic = true;

            equippedData = data;
            equippedIndex = idx;
        }

        public void UnequipToInventory()
        {
            if (!HasItem) return;
            Destroy(equippedInstance);
            equippedInstance = null;
            equippedData = null;
            equippedIndex = -1;
        }

        public void DropOneFromEquipped()
        {
            if (!HasItem || equippedIndex < 0) return;

            var stack = slots[equippedIndex];
            if (stack == null || stack.data == null || stack.data.worldPrefab == null) return;

            // referensi arah dari kamera (kalau ada)
            Transform refT = mainCam ? mainCam.transform : playerBody;

            Vector3 spawnPos = refT.position + refT.forward * dropForward + Vector3.up * dropUp;
            Quaternion spawnRot = Quaternion.LookRotation(refT.forward, Vector3.up);

            GameObject go = Instantiate(stack.data.worldPrefab, spawnPos, spawnRot);

            // pastikan layer dunia
            SetLayerRecursively(go, LayerMask.NameToLayer("Default"));

            // pastikan fisika
            var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;

            // warisi kecepatan player + dorong ke depan
            var pRb = playerBody ? playerBody.GetComponent<Rigidbody>() : null;
            if (pRb) rb.linearVelocity = pRb.linearVelocity;
            rb.AddForce(refT.forward * dropThrowForce, ForceMode.Impulse);

            // kurangi stack
            stack.count -= 1;
            if (stack.count <= 0)
            {
                slots.RemoveAt(equippedIndex);
                equippedIndex = -1;

                // hilangkan viewmodel
                Destroy(equippedInstance);
                equippedInstance = null;
                equippedData = null;

                // auto-equip slot pertama kalau ada
                if (slots.Count > 0) EquipByIndex(0);
            }
        }

        // ====== Helpers ======
        void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}
