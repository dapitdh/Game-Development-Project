using UnityEngine;
using TMPro;

public class Sc_pickupItem : MonoBehaviour
{
    public Transform itemHolder;           // drag dari Hero -> itemHolder
    public float pickupRange = 3f;         // jarak pickup maksimal
    public LayerMask itemLayer;            // pastikan layer FlashLight ikut di sini

    private GameObject heldItem;
    private WeaponShooter heldWeapon;      // cache senjata (jika item adalah senjata)

    public Vector3 targetPos = Vector3.zero;

    // UI
    public GameObject presEUI, dropGUI;
    public GameObject findCrowbarGUI, findKeyCardGUI;
    public GameObject pressGUI;            // ← UI "Press G to pickup FlashLight"

    // Pose khusus FlashLight (agar pas di tangan)
    public Vector3 flashlightLocalPos = new Vector3(0.25f, -0.25f, 0.45f);
    public Vector3 flashlightLocalEuler = new Vector3(0f, 0f, 0f);

    void Update()
    {
        // Jika sudah pegang item -> hanya cek drop
        if (heldItem != null)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                DropItem();
                if (dropGUI) dropGUI.SetActive(false);
            }
            return;
        }

        var cam = Camera.main;
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

            // --- Pickup FlashLight dengan G ---
            if (hit.collider.CompareTag("FlashLight"))
            {
                if (pressGUI) pressGUI.SetActive(true);
                if (presEUI) presEUI.SetActive(false);
                if (findCrowbarGUI) findCrowbarGUI.SetActive(false);
                if (findKeyCardGUI) findKeyCardGUI.SetActive(false);

                if (Input.GetKeyDown(KeyCode.G))
                {
                    PickUpFlashLight(hit.collider.gameObject);
                    if (pressGUI) pressGUI.SetActive(false);
                    if (dropGUI) dropGUI.SetActive(true);
                }
                return;
            }
            else
            {
                if (pressGUI) pressGUI.SetActive(false);
            }

            // --- Pickup item umum dengan E ---
            if (hit.collider.CompareTag("Item") ||
                hit.collider.CompareTag("KeyCard") ||
                hit.collider.CompareTag("Obstacle") ||
                hit.collider.CompareTag("KayuPenghalang"))
            {
                if (!hit.collider.CompareTag("KayuPenghalang"))
                {
                    if (presEUI) presEUI.SetActive(true);
                }
                else
                {
                    if (findCrowbarGUI) findCrowbarGUI.SetActive(true);
                }

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
            if (pressGUI) pressGUI.SetActive(false);
            if (findCrowbarGUI) findCrowbarGUI.SetActive(false);
            if (findKeyCardGUI) findKeyCardGUI.SetActive(false);
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red);
        }
    }

    void PickUpItem(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider[] cols = item.GetComponentsInChildren<Collider>(true);

        if (rb) rb.isKinematic = true;
        foreach (var col in cols)
        {
            if (col is SphereCollider) continue;
            col.enabled = false;
        }

        item.transform.SetParent(itemHolder);
        if (item.name == "crowbar")
            item.transform.localPosition = targetPos + new Vector3(0, -1f, 0);
        else
            item.transform.localPosition = targetPos;

        item.transform.localRotation = Quaternion.identity;

        heldItem = item;

        heldWeapon = heldItem.GetComponent<WeaponShooter>()
                     ?? heldItem.GetComponentInChildren<WeaponShooter>(true);
        if (heldWeapon) heldWeapon.OnPickedUp(itemHolder);

        Debug.Log("Picked up: " + item.name);
    }

    // === Pickup khusus FlashLight (G) ===
    void PickUpFlashLight(GameObject flashGo)
    {
        Rigidbody rb = flashGo.GetComponent<Rigidbody>();
        Collider[] cols = flashGo.GetComponentsInChildren<Collider>(true);

        if (rb) rb.isKinematic = true;
        foreach (var col in cols)
        {
            if (col is SphereCollider) continue;
            col.enabled = false;
        }

        flashGo.transform.SetParent(itemHolder);
        flashGo.transform.localPosition = flashlightLocalPos;
        flashGo.transform.localRotation = Quaternion.Euler(flashlightLocalEuler);
        flashGo.transform.localScale = Vector3.one;

        heldItem = flashGo;

        Debug.Log("Picked up FlashLight");
    }

    public void DropItem()
    {
        if (heldItem == null) return;

        if (heldWeapon) heldWeapon.OnDropped();

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        Collider[] cols = heldItem.GetComponentsInChildren<Collider>(true);

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
        heldWeapon = null;
    }
}
