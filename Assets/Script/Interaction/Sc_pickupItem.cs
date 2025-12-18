using UnityEngine;
using TMPro;

public class Sc_pickupItem : MonoBehaviour
{
    public Transform itemHolder;
    public float pickupRange = 3f;
    public LayerMask itemLayer;
    private GameObject heldItem;
    private WeaponShooter heldWeapon;
    public Vector3 targetPos = Vector3.zero;

    [Header("UI")]
    public GameObject presEUI;              // "Press E" item umum
    public GameObject dropGUI;
    public GameObject findCrowbarGUI;
    public GameObject findKeyCardGUI;

    public GameObject pressFlashlightGUI;   // "Press G" untuk flashlight
    public GameObject pressChestGUI;        // "Press G" untuk chest
    public GameObject pressCartGUI;         // "Press E" untuk kereta

    [Header("Flashlight")]
    public Vector3 flashlightLocalPos = new Vector3(0.25f, -0.25f, 0.45f);
    public Vector3 flashlightLocalEuler = new Vector3(0f, 0f, 0f);

    void Update()
    {
        // Kalau lagi pegang item → hanya bisa drop
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

        Ray ray = cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

            // Matikan dulu semua UI interaksi
            HideAllInteractionUI();

            // ========== FLASHLIGHT (G) ==========
            if (hit.collider.CompareTag("FlashLight"))
            {
                if (pressFlashlightGUI) pressFlashlightGUI.SetActive(true);

                if (Input.GetKeyDown(KeyCode.G))
                {
                    PickUpFlashLight(hit.collider.gameObject);
                    if (pressFlashlightGUI) pressFlashlightGUI.SetActive(false);
                    if (dropGUI) dropGUI.SetActive(true);
                }
                return;
            }

            // ========== CHEST (G untuk buka peti) ==========
            if (hit.collider.CompareTag("Chest") ||
                hit.collider.transform.root.CompareTag("Chest"))
            {
                if (pressChestGUI) pressChestGUI.SetActive(true);

                if (Input.GetKeyDown(KeyCode.G))
                {
                    ChestOpener chest = hit.collider.GetComponentInParent<ChestOpener>();
                    if (chest) chest.OpenChest();
                }
                return;
            }

            // ========== ITEM / OBSTACLE / KEYCARD (E) ==========
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
                return;
            }

            // ========== KERETA (E untuk dorong – GUI saja) ==========
            if (hit.collider.CompareTag("Minecart") ||
                hit.collider.transform.root.CompareTag("Minecart"))
            {
                if (pressCartGUI) pressCartGUI.SetActive(true);
                return;
            }

            // ========== PIN LOCK ==========
            if (hit.collider.name == "pinLock")
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    var pin = hit.collider.GetComponent<Sc_showUIPin>();
                    Debug.Log(pin);
                    if (pin) pin.isClicked = true;
                }
                return;
            }

            // ========== ELECTRIC DOOR ==========
            if (hit.collider.name == "electric door")
            {
                if (findKeyCardGUI) findKeyCardGUI.SetActive(true);
                return;
            }

            // selain kasus di atas, UI sudah dimatikan oleh HideAllInteractionUI()
        }
        else
        {
            HideAllInteractionUI();
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red);
        }
    }

    void HideAllInteractionUI()
    {
        if (presEUI) presEUI.SetActive(false);
        if (pressFlashlightGUI) pressFlashlightGUI.SetActive(false);
        if (pressChestGUI) pressChestGUI.SetActive(false);
        if (pressCartGUI) pressCartGUI.SetActive(false);
        if (findCrowbarGUI) findCrowbarGUI.SetActive(false);
        if (findKeyCardGUI) findKeyCardGUI.SetActive(false);
        // dropGUI tetap hanya diatur saat pegang/drop item
    }

    //=================== PICKUP ITEM ===================

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
