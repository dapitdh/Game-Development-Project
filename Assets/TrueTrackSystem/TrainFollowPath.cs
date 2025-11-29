using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MinecartPingPongSimple : MonoBehaviour
{
    [Header("Waypoints (isi P1, P2, P3, ... urut sepanjang rel)")]
    public Transform[] waypoints;

    [Header("Movement")]
    public float speed = 5f;
    public float rotateSpeed = 5f;
    public bool pingPong = true;   // true = maju-mundur

    private int currentIndex = 0;  // index waypoint tujuan
    private int direction = 1;     // 1 = ke index naik, -1 = ke index turun

    // segmen untuk rotasi (arah rel)
    private int segStartIndex = 0;
    private int segEndIndex = 1;

    [Header("Horn / Klakson Kereta")]
    public AudioSource hornAudioSource;      // boleh di-assign manual, kalau kosong akan diambil dari komponen sendiri
    public float hornIntervalMin = 3f;       // detik
    public float hornIntervalMax = 7f;       // detik
    public bool hornEnabled = true;

    // Player cuma dengar horn kalau di dalam trigger collider milik minecart_v2
    private bool playerInHornZone = false;

    private float hornTimer = 0f;
    private float currentHornInterval = 0f;

    void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("MinecartPingPongSimple: butuh minimal 2 waypoint.");
            enabled = false;
            return;
        }

        if (hornAudioSource == null)
        {
            hornAudioSource = GetComponent<AudioSource>();
        }

        // pastikan audio 3D
        if (hornAudioSource != null)
        {
            hornAudioSource.spatialBlend = 1f; // full 3D
        }

        // mulai di waypoint pertama
        transform.position = waypoints[0].position;

        // target awal: waypoint berikutnya
        currentIndex = 1;
        direction = 1;

        segStartIndex = 0;
        segEndIndex = 1;

        UpdateRotationImmediate();
        ResetHornTimer();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        // ---- Gerakan kereta ----
        Transform target = waypoints[currentIndex];
        Vector3 targetPos = target.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        UpdateRotationSmooth();

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            int prevIndex = currentIndex;

            if (pingPong)
            {
                // Pola: 0 -> 1 -> 2 -> ... -> last -> ... -> 2 -> 1 -> 0 -> 1 ...
                if (direction > 0)
                {
                    if (currentIndex >= waypoints.Length - 1)
                    {
                        direction = -1;
                        currentIndex = waypoints.Length - 2;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }
                else
                {
                    if (currentIndex <= 0)
                    {
                        direction = 1;
                        currentIndex = 1;
                    }
                    else
                    {
                        currentIndex--;
                    }
                }
            }
            else
            {
                currentIndex++;
                if (currentIndex >= waypoints.Length)
                {
                    currentIndex = 0;
                }
            }

            segStartIndex = Mathf.Min(prevIndex, currentIndex);
            segEndIndex = Mathf.Max(prevIndex, currentIndex);
        }

        // ---- Horn / Klakson ----
        UpdateHorn();
    }

    void UpdateRotationSmooth()
    {
        Vector3 from = waypoints[segStartIndex].position;
        Vector3 to = waypoints[segEndIndex].position;

        Vector3 forward = (to - from).normalized;
        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }

    void UpdateRotationImmediate()
    {
        Vector3 from = waypoints[segStartIndex].position;
        Vector3 to = waypoints[segEndIndex].position;

        Vector3 forward = (to - from).normalized;
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    // ================== HORN ==================

    void ResetHornTimer()
    {
        if (!hornEnabled) return;

        if (hornIntervalMax < hornIntervalMin)
        {
            hornIntervalMax = hornIntervalMin;
        }

        currentHornInterval = Random.Range(hornIntervalMin, hornIntervalMax);
        hornTimer = currentHornInterval;
    }

    void UpdateHorn()
    {
        if (!hornEnabled) return;
        if (hornAudioSource == null) return;

        // hanya bunyi kalau player sedang di dalam trigger minecart_v2
        if (!playerInHornZone) return;

        hornTimer -= Time.deltaTime;
        if (hornTimer <= 0f)
        {
            hornAudioSource.Play();
            ResetHornTimer();
        }
    }

    // ================== TRIGGER ZONE DI MINECART ==================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInHornZone = true;
            // Debug.Log("Player masuk zona horn kereta");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInHornZone = false;
            // Debug.Log("Player keluar zona horn kereta");
        }
    }
}
