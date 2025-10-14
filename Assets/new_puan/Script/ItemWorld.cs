using UnityEngine;

namespace FPP
{
    public class ItemWorld : MonoBehaviour
    {
        public ItemData data;
        public int quantity = 1;

        void Reset()
        {
            if (!TryGetComponent<Rigidbody>(out _)) gameObject.AddComponent<Rigidbody>();
            if (!TryGetComponent<Collider>(out var col)) col = gameObject.AddComponent<BoxCollider>();
            if (col is MeshCollider mc) mc.convex = true;
            else col.isTrigger = false;
        }
    }
}
