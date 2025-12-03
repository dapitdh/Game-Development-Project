using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider))]  
[RequireComponent(typeof(Rigidbody))]

public class MinecartPingPong_inside : MonoBehaviour
{
    [Header("Waypoints (isi P1, P2, P3, ... urut sepanjang rel)")]
    public Transform[] waypoints;

    [Header("Movement")]
    public float speed = 5f;
    public float rotateSpeed = 5f;
    public bool pingPong = true;   // true = maju-mundur

    [Header("Push Control")]
    [Tooltip("Kalau true, kereta hanya jalan kalau didorong (dipanggil dari MinecartPushZone). Kalau false, kereta auto jalan.")]
    public bool usePushControl = true;

    // index waypoint tujuan
    private int currentIndex = 0;
    // 1 = index naik, -1 = index turun
    private int direction = 1;

    // segmen untuk rotasi (arah rel)
    private int segStartIndex = 0;
    private int segEndIndex = 1;

    // status apakah sekarang sedang didorong
    private bool isBeingPushed = false;

    [Header("Buka Pintu Rahasia")]
    [Tooltip("Drag GameObject pintu (tembok) yang punya SecretDoorSimple di sini")]
    public SecretDoorSimple secretDoor;

    [Tooltip("Index waypoint yang memicu pintu terbuka. -1 = otomatis waypoint terakhir")]
    public int doorOpenWaypointIndex = -1;

    [Tooltip("Kalau true, pintu hanya dibuka sekali saja.")]
    public bool openDoorOnlyOnce = true;

    private bool doorAlreadyOpened = false;

    [Header("Horn / Klakson Kereta")]
    public AudioSource hornAudioSource;      // boleh di-assign manual, kalau kosong akan diambil dari komponen sendiri
    public float hornIntervalMin = 3f;       // detik
    public float hornIntervalMax = 7f;       // detik
    public bool hornEnabled = true;

    // Player cuma dengar horn kalau di dalam trigger collider milik minecart
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

        // kalau -1 / out of range → pakai waypoint terakhir sebagai pemicu pintu
        if (doorOpenWaypointIndex < 0 || doorOpenWaypointIndex >= waypoints.Length)
        {
            doorOpenWaypointIndex = waypoints.Length - 1;
        }

        // rigidbody kinematic supaya kontrol posisi dari script
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        UpdateRotationImmediate();
        ResetHornTimer();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        // ---- Gerakan kereta ----
        Transform target = waypoints[currentIndex];
        Vector3 targetPos = target.position;

        float usedSpeed = speed;

        if (usePushControl)
        {
            // kalau mode dorong dan tidak sedang didorong → diam
            if (!isBeingPushed)
            {
                usedSpeed = 0f;
            }
        }

        if (usedSpeed > 0f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                usedSpeed * Time.deltaTime
            );
        }

        UpdateRotationSmooth();

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // waypoint yang baru saja dicapai
            int reachedIndex = currentIndex;

            // cek apakah waypoint ini yang harus membuka pintu
            CheckOpenDoorOnWaypoint(reachedIndex);

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

    void CheckOpenDoorOnWaypoint(int reachedIndex)
    {
        if (secretDoor == null) return;
        if (openDoorOnlyOnce && doorAlreadyOpened) return;

        if (reachedIndex == doorOpenWaypointIndex)
        {
            secretDoor.OpenDoor();
            doorAlreadyOpened = true;
        }
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

        // hanya bunyi kalau player sedang di dalam trigger minecart
        if (!playerInHornZone) return;

        hornTimer -= Time.deltaTime;
        if (hornTimer <= 0f)
        {
            hornAudioSource.Play();
            ResetHornTimer();
        }
    }

    // ================== TRIGGER ZONE HORN ==================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInHornZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInHornZone = false;
        }
    }

    // ================== DIPANGGIL DARI SCRIPT PUSH ==================

    public void SetPushing(bool pushing)
    {
        isBeingPushed = pushing;
    }
}
