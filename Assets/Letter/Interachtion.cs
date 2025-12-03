using UnityEngine;

public class Interaction : MonoBehaviour
{
    public float interactionDistance = 3f;
    public GameObject interactionText;     // tip "Q: Read / Q: Close"
    public LayerMask layers = ~0;

    private Camera cam;
    private Letter_sc openLetter;          // surat yang sedang terbuka

    void Awake()
    {
        cam = Camera.main;
        if (!cam) cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // Kalau ada surat terbuka: Q = Close, tanpa raycast
        if (openLetter && openLetter.IsOpen)
        {
            if (interactionText) interactionText.SetActive(true); // bisa ganti text ke "Q: Close"
            if (Input.GetKeyDown(KeyCode.Q))
            {
                openLetter.Close();
                openLetter = null;
            }
            return;
        }

        if (interactionText) interactionText.SetActive(false);
        if (!cam) return;

        // Raycast dari kamera ke depan
        if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                            out RaycastHit hit, interactionDistance, layers))
        {
            // Cari Letter_sc di collider, parent, atau children
            Letter_sc letter =
                hit.collider.GetComponent<Letter_sc>() ??
                hit.collider.GetComponentInParent<Letter_sc>() ??
                hit.collider.GetComponentInChildren<Letter_sc>();

            if (letter != null && hit.collider.CompareTag("Letter"))
            {
                Debug.Log($"[Interaction] Hit {hit.collider.name}");

                if (interactionText) interactionText.SetActive(true); // "Q: Read"
                if (Input.GetKeyDown(KeyCode.Q)) // toggle Q
                {
                    // Tutup yang lama jika beda
                    if (openLetter && openLetter != letter && openLetter.IsOpen)
                        openLetter.Close();

                    if (letter.IsOpen)  // kalau yang disorot sudah terbuka → tutup
                    {
                        letter.Close();
                        openLetter = null;
                    }
                    else                // kalau belum terbuka → buka
                    {
                        letter.Open();
                        openLetter = letter;
                    }
                }
            }
        }
    }
}
