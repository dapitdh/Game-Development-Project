using UnityEngine;
using TMPro;

public class Sc_pickupItem : MonoBehaviour
{
    public Transform itemHolder;    // drag dari Hero -> itemHolder
    public float pickupRange = 3f;  // jarak pickup maksimal
    public LayerMask itemLayer;     // layer "Item", biar raycast fokus

    private GameObject heldItem;
    private WeaponShooter heldWeapon;   // ← NEW: cache senjata (jika item adalah senjata)

    public Vector3 targetPos = new Vector3(0, 0, 0);

    // public TextMeshProUGUI pickupText;
    public GameObject presEUI, dropGUI;
    public GameObject findCrowbarGUI, findKeyCardGUI;

    void Update()
    {
        // Kalau sudah pegang item -> cek tombol drop
        if (heldItem != null)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                DropItem();
                if (dropGUI) dropGUI.SetActive(false);
            }
            return; // NOTE: raycast di-skip saat sedang pegang item
        }

        // === Ray dari titik tengah layar ===
        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f, 0)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer))
        {
            if (hit.collider.CompareTag("Item") ||
                hit.collider.CompareTag("KeyCard") ||
                hit.collider.CompareTag("Obstacle") ||
                hit.collider.CompareTag("KayuPenghalang"))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

                if (!hit.collider.CompareTag("KayuPenghalang"))
                {
                    if (presEUI) presEUI.SetActive(true);
                }
                else
                {
                    if (findCrowbarGUI) findCrowbarGUI.SetActive(true);
                }

                // Saat tekan E, ambil item
                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpItem(hit.collider.gameObject);
                    if (presEUI) presEUI.SetActive(false);
                    if (dropGUI) dropGUI.SetActive(true);
                }
            }
            else if (hit.collider.name == "electric door")
            {
                if (findKeyCardGUI) findKeyCardGUI.SetActive(true);
            }
            else
            {
                if (presEUI) presEUI.SetActive(false);
                if (dropGUI) dropGUI.SetActive(false);
                if (findCrowbarGUI) findCrowbarGUI.SetActive(false);
                if (findKeyCardGUI) findKeyCardGUI.SetActive(false);
            }
        }
        else
        {
            if (presEUI) presEUI.SetActive(false);
            if (findCrowbarGUI) findCrowbarGUI.SetActive(false);
            if (findKeyCardGUI) findKeyCardGUI.SetActive(false);
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red);
        }
    }

    void PickUpItem(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider[] cols = item.GetComponents<Collider>();

        // Nonaktifkan physics saat dipegang
        if (rb) rb.isKinematic = true;
        foreach (var col in cols)
        {
            if (col is SphereCollider) continue; // kalau item butuh sphere trigger dsb.
            col.enabled = false;
        }

        // Pindahkan ke tangan
        item.transform.SetParent(itemHolder);
        if (item.name == "crowbar")
            item.transform.localPosition = targetPos + new Vector3(0, -1f, 0);
        else
            item.transform.localPosition = targetPos;

        item.transform.localRotation = Quaternion.identity;

        heldItem = item;

        // ← NEW: beri tahu script senjata (jika ada)
        heldWeapon = heldItem.GetComponent<WeaponShooter>()
                     ?? heldItem.GetComponentInChildren<WeaponShooter>(true);
        if (heldWeapon) heldWeapon.OnPickedUp(itemHolder);

        Debug.Log("Picked up: " + item.name);
    }

    public void DropItem()
    {
        if (heldItem == null) return;

        // ← NEW: beri tahu senjata lebih dulu
        if (heldWeapon) heldWeapon.OnDropped();

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        Collider[] cols = heldItem.GetComponents<Collider>();

        heldItem.transform.SetParent(null);

        foreach (Collider col in cols)
            col.enabled = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.AddForce(Camera.main.transform.forward * 4f, ForceMode.Impulse);
        }

        Debug.Log("Dropped: " + heldItem.name);
        heldItem = null;
        heldWeapon = null; // ← NEW: bersihkan cache
    }
}
