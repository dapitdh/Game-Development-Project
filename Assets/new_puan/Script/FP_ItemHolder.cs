using UnityEngine;

namespace FPP
{
    public class FP_ItemHolder : MonoBehaviour
    {
        [Header("Refs")]
        public Transform socket;                // drag: ItemHolder
        public GameObject defaultItemPrefab;    // drag: prefab FlashLight

        [Header("Spawn Pose")]
        public Vector3 localPos = new Vector3(0.25f, -0.25f, 0.5f);
        public Vector3 localEuler = Vector3.zero;

        [Header("Layer")]
        public string fpLayerName = "FP_Arms";  // layer item FPP

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
            currentItem.transform.localPosition = localPos;
            currentItem.transform.localEulerAngles = localEuler;
            currentItem.transform.localScale = Vector3.one;

            // pastikan layer benar
            int layer = LayerMask.NameToLayer(fpLayerName);
            if (layer >= 0) SetLayerRecursively(currentItem, layer);

            // matikan collider agar gak tabrakan
            foreach (var col in currentItem.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // optional: matikan rigidbody
            foreach (var r in currentItem.GetComponentsInChildren<Rigidbody>())
                r.isKinematic = true;
        }

        void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
                SetLayerRecursively(t.gameObject, layer);
        }
    }
}
