using UnityEngine;

[DisallowMultipleComponent]
public class FootstepSFX : MonoBehaviour
{
    [Header("Audio Source (3D)")]
    [Tooltip("AudioSource untuk memutar SFX. Kalau kosong, akan dibuat otomatis di GameObject ini.")]
    public AudioSource source;

    [Header("Clips")]
    public AudioClip[] walkClips;   // isi beberapa clip langkah (random)
    public AudioClip[] runClips;    // opsional; kalau kosong, pakai walkClips
    public AudioClip jumpClip;      // opsional
    public AudioClip landClip;      // opsional

    [Header("Ground Check")]
    [Tooltip("Drag 'GroundCheck' milik player (sesuai hierarchy kamu).")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0; // default: semua layer

    [Header("Movement & Step Tuning")]
    [Tooltip("Kecepatan minimum dianggap bergerak (m/s).")]
    public float minMoveSpeed = 0.1f;
    [Tooltip("Jarak horizontal per 1 langkah saat JALAN (meter).")]
    public float walkStrideLength = 1.9f;
    [Tooltip("Jarak horizontal per 1 langkah saat LARI (meter).")]
    public float runStrideLength = 2.6f;
    [Tooltip("Volume langkah jalan.")]
    public float walkVolume = 0.6f;
    [Tooltip("Volume langkah lari.")]
    public float runVolume = 0.85f;
    [Tooltip("Jitter pitch supaya tidak monoton (min..max).")]
    public Vector2 pitchJitter = new Vector2(0.95f, 1.05f);

    [Header("Input")]
    [Tooltip("Tahan Shift untuk lari.")]
    public bool useShiftToRun = true;

    // --- internal ---
    CharacterController _cc;
    bool _isGrounded, _wasGrounded;
    Vector3 _prevPos;
    float _accumDist; // akumulasi jarak horizontal

    void Reset()
    {
        // Auto-setup kalau drop pertama kali
        if (!source)
        {
            source = GetComponent<AudioSource>();
            if (!source) source = gameObject.AddComponent<AudioSource>();
        }
        if (!groundCheck)
        {
            var t = transform.Find("GroundCheck");
            if (t) groundCheck = t;
        }
        source.spatialBlend = 1f;  // 3D
        source.playOnAwake = false;
        source.loop = false;
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!source)
        {
            source = GetComponent<AudioSource>();
            if (!source) source = gameObject.AddComponent<AudioSource>();
        }
        source.spatialBlend = 1f;
        source.playOnAwake = false;
        source.loop = false;
        _prevPos = transform.position;
    }

    void Update()
    {
        // Hormati sistem pause-mu
        if (MenuPause.GameIsPaused)
        {
            if (source && source.isPlaying) source.Pause();
            _prevPos = transform.position;   // reset delta
            _wasGrounded = _isGrounded;
            return;
        }
        else if (source) source.UnPause();

        // --- Ground check ---
        _isGrounded = CheckGrounded();

        // --- Jump SFX (on press while grounded) ---
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            PlayOne(jumpClip, Mathf.Max(walkVolume, 0.7f));

        // --- Landing SFX (on land) ---
        if (!_wasGrounded && _isGrounded)
            PlayOne(landClip, Mathf.Max(walkVolume * 1.1f, 0.7f));

        // --- Hitung jarak horizontal frame ini ---
        Vector3 frameDelta = transform.position - _prevPos;
        Vector3 planar = Vector3.ProjectOnPlane(frameDelta, Vector3.up);
        float dist = planar.magnitude;
        float speed = (Time.deltaTime > 0f) ? dist / Time.deltaTime : 0f;

        bool moving = speed > minMoveSpeed;
        bool running = useShiftToRun && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        // --- Footsteps ---
        if (_isGrounded && moving)
        {
            _accumDist += dist;
            float stride = running ? runStrideLength : walkStrideLength;

            if (_accumDist >= stride)
            {
                PlayFootstep(running);
                _accumDist = 0f;
            }
        }
        else
        {
            // pelan-pelan turunkan akumulasi (biar tidak langsung nol)
            _accumDist = Mathf.Clamp(_accumDist - (walkStrideLength * 0.5f * Time.deltaTime), 0f, 999f);
        }

        _prevPos = transform.position;
        _wasGrounded = _isGrounded;
    }

    bool CheckGrounded()
    {
        if (_cc) return _cc.isGrounded;

        if (groundCheck)
        {
            // Sphere Check di titik GroundCheck
            return Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        }

        // fallback raycast dari sedikit di atas kaki
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        return Physics.Raycast(origin, Vector3.down, 0.4f, groundMask, QueryTriggerInteraction.Ignore);
    }

    void PlayFootstep(bool running)
    {
        AudioClip[] bank = (running && runClips != null && runClips.Length > 0) ? runClips : walkClips;
        if (bank == null || bank.Length == 0 || source == null) return;

        source.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        var clip = bank[Random.Range(0, bank.Length)];
        float vol = running ? runVolume : walkVolume;
        source.PlayOneShot(clip, vol);
    }

    void PlayOne(AudioClip clip, float vol)
    {
        if (!clip || source == null) return;
        source.pitch = 1f;
        source.PlayOneShot(clip, vol);
    }
}
