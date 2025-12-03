using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class MinecartPushZone : MonoBehaviour
{
    [Header("Reference Kereta")]
    public MinecartPingPong_inside minecart;

    [Header("Pengaturan Push")]
    [Tooltip("Kalau true, harus menahan W + E untuk mendorong. Kalau false, cukup E.")]
    public bool requireForwardKey = true;

    private bool playerInside = false;

    private void Awake()
    {
        // Pastikan collider jadi trigger
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (minecart != null)
                minecart.SetPushing(false);
        }
    }

    private void Update()
    {
        if (!playerInside || minecart == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Tombol utama push: E
        bool pushHeld = kb.eKey.isPressed;

        // Optional: harus sambil menahan W (jalan maju)
        if (requireForwardKey)
        {
            pushHeld = pushHeld && kb.wKey.isPressed;
        }

        minecart.SetPushing(pushHeld);
    }
}
