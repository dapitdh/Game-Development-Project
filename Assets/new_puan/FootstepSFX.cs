using UnityEngine;
using FPP;  

[RequireComponent(typeof(AudioSource))]
public class FootstepSFX : MonoBehaviour
{
    [Header("Referensi")]
    public Puan_control puan;  

    [Header("Footstep Clips")]
    public AudioClip[] walkClips;   
    public AudioClip[] runClips;   

    [Header("Interval Langkah (detik)")]
    public float walkStepInterval = 0.5f;   
    public float runStepInterval = 0.3f;    

    [Header("Threshold Kecepatan")]
    [Tooltip("Di bawah ini dianggap diam")]
    public float minMoveSpeed = 0.1f;

    private AudioSource audioSource;
    private float nextStepTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;

        nextStepTime = 0f;
    }

    void Update()
    {
        if (puan == null) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        Vector2 hv = new Vector2(v.x, v.z);
        float speed = hv.magnitude;
        bool isMoving = speed > minMoveSpeed;
        bool isGrounded = puan.IsGrounded;
        bool isSprinting = puan.IsSprinting;

        if (!isGrounded || !isMoving)
        {
            nextStepTime = Time.time;
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }
        float interval = isSprinting ? runStepInterval : walkStepInterval;

        AudioClip[] bank = isSprinting && runClips != null && runClips.Length > 0
            ? runClips
            : walkClips;

        if (bank == null || bank.Length == 0) return;

        if (Time.time >= nextStepTime)
        {
            PlayFootstep(bank);
            nextStepTime = Time.time + interval;
        }
    }

    void PlayFootstep(AudioClip[] bank)
    {
        int index = Random.Range(0, bank.Length);
        AudioClip clip = bank[index];

        audioSource.clip = clip;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.volume = Random.Range(0.9f, 1f);

        audioSource.Play();
    }
}