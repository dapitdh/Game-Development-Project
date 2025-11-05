using UnityEngine;

namespace FPP
{
    public class FP_ItemHolder : MonoBehaviour
    {
        [Header("Refs")]
        public Transform socket;
        public GameObject defaultItemPrefab;

        [Header("Spawn Pose")]
        // Geser default ke kanan (x dari 0.25 -> 0.35)
        public Vector3 localPos = new Vector3(0.35f, -0.55f, 0.5f);
        public Vector3 localEuler = Vector3.zero;

        [Header("Layer")]
        public string fpLayerName = "FP_Arms";

        [Header("Fine Tune")]
        [Tooltip("Geser horizontal (+ = kanan, - = kiri) di atas localPos.x")]
        [Range(-0.25f, 0.25f)] public float rightOffset = 0.08f;

        GameObject currentItem;

        void Start()
        {
            if (socket == null) socket = transform;
            if (defaultItemPrefab) Equip(defaultItemPrefab);
        }

        public void Equip(GameObject prefab)
        {
            if (currentItem) Destroy(currentItem);

            currentItem = Instantiate(prefab, socket);

            // Terapkan pose + offset
            ApplyPose();

            // Set layer ke seluruh child
            int layer = LayerMask.NameToLayer(fpLayerName);
            if (layer >= 0) SetLayerRecursively(currentItem, layer);

            // Nonaktifkan fisika
            foreach (var col in currentItem.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var r in currentItem.GetComponentsInChildren<Rigidbody>())
                r.isKinematic = true;
        }

        void ApplyPose()
        {
            if (!currentItem) return;

            var t = currentItem.transform;
            t.localPosition = localPos + new Vector3(rightOffset, 0f, 0f);
            t.localEulerAngles = localEuler;
            t.localScale = Vector3.one;
        }

        void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

#if UNITY_EDITOR
        // Auto update saat ubah nilai di Inspector
        void OnValidate()
        {
            if (currentItem) ApplyPose();
        }
#endif
    }
}
