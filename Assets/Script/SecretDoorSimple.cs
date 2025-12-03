using UnityEngine;

public class SecretDoorSimple : MonoBehaviour
{
    [Header("Gerak Pintu")]
    [Tooltip("Seberapa jauh pintu bergerak dari posisi awal saat terbuka")]
    public float openDistance = 5f;

    [Tooltip("Kecepatan buka pintu")]
    public float openSpeed = 2f;

    [Tooltip("Arah pergerakan pintu saat terbuka (default: turun ke bawah)")]
    public Vector3 openDirection = Vector3.down;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool opening = false;
    private Collider col;

    private void Awake()
    {
        closedPos = transform.position;
        openDirection = openDirection.normalized;
        openPos = closedPos + openDirection * openDistance;

        col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!opening) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            openPos,
            openSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, openPos) < 0.01f)
        {
            // Optional: matikan collider supaya player bisa lewat
            if (col != null)
                col.enabled = false;
        }
    }

    // Dipanggil dari script kereta saat kereta sampai di waypoint tertentu
    public void OpenDoor()
    {
        opening = true;
    }
}
