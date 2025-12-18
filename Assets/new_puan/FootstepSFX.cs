using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSFX : MonoBehaviour
{
    [Header("Ground Check (samakan dengan Puan_control)")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundCheckRadius = 0.2f;

    [Header("Footstep Settings")]
    public AudioClip[] footstepClips;
    public float stepIntervalWalking = 0.5f;
    public float stepIntervalRunning = 0.3f;

    [Header("Threshold Kecepatan")]
    public float walkingSpeedThreshold = 0.1f;
    public float runningSpeedThreshold = 4f;

    private AudioSource audioSource;
    private float stepCycle;
    private float nextStep;

    private Vector3 lastPosition;
    private float currentSpeed;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;

        lastPosition = transform.position;
    }

    void Update()
    {
        // --- Hitung speed ---
        Vector3 currentPosition = transform.position;
        Vector3 horizontalDelta = new Vector3(
            currentPosition.x - lastPosition.x,
            0f,
            currentPosition.z - lastPosition.z
        );
        currentSpeed = horizontalDelta.magnitude / Time.deltaTime;
        lastPosition = currentPosition;

        // --- Cek grounded ---
        bool isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // Jika tidak di tanah atau sangat pelan -> reset dan STOP audio
        if (!isGrounded || currentSpeed < walkingSpeedThreshold)
        {
            stepCycle = 0f;
            nextStep = 0f;

            // pastikan tidak ada suara langkah yang lanjut
            if (audioSource.isPlaying)
                audioSource.Stop();

            return;
        }

        // --- Jalan / lari? ---
        float stepInterval = (currentSpeed >= runningSpeedThreshold)
            ? stepIntervalRunning
            : stepIntervalWalking;

        stepCycle += currentSpeed * Time.deltaTime;

        if (stepCycle > nextStep)
        {
            PlayFootstep();
            nextStep = stepCycle + stepInterval;
        }
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int n = Random.Range(0, footstepClips.Length);
        AudioClip clip = footstepClips[n];

        // Pakai Play() biasa, bukan PlayOneShot, supaya bisa di-Stop
        audioSource.clip = clip;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.volume = Random.Range(0.8f, 1f);
        audioSource.Play();
    }
}