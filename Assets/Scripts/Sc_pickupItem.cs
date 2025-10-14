using UnityEngine;

public class Sc_pickupItem : MonoBehaviour
{
    public Transform itemHolder;    // drag dari Hero -> itemHolder
    public float pickupRange = 3f;  // jarak pickup maksimal
    public LayerMask itemLayer;     // layer "Item", biar raycast fokus
    private GameObject heldItem;

    void Update()
    {
        // Kalau sudah pegang item -> cek tombol drop
        if (heldItem != null)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                DropItem();
            }
            return;
        }

        // === Ray dari titik tengah layar ===
        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f, 0)
        );
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer))
        {
            if (hit.collider.CompareTag("Item"))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
                // Debug.Log("Lihat item: " + hit.collider.name);

                // Saat tekan E, ambil item
                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpItem(hit.collider.gameObject);
                }
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red);
        }
    }

    void PickUpItem(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider[] cols = item.GetComponents<Collider>();

        // Nonaktifkan physics
        if (rb) rb.isKinematic = true;
        foreach (var col in cols)
        {
            if (col is SphereCollider) continue; // jangan dimatikan
            col.enabled = false;                  // lainnya dimatikan
        }


        // Pindahkan ke tangan
        item.transform.SetParent(itemHolder);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        heldItem = item;

        Debug.Log("Picked up: " + item.name);
    }

    void DropItem()
    {
        if (heldItem == null) return;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        Collider[] cols = heldItem.GetComponents<Collider>();

        heldItem.transform.SetParent(null);

        foreach (Collider col in cols)
        {
            col.enabled = true;
        }

        if (rb)
        {
            rb.isKinematic = false;
            rb.AddForce(Camera.main.transform.forward * 4f, ForceMode.Impulse);
        }

        Debug.Log("Dropped: " + heldItem.name);
        heldItem = null;
    }
}
