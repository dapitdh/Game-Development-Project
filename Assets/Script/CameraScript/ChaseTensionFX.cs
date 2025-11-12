using UnityEngine;
using UnityEngine.UI;

public class ChaseTensionFX : MonoBehaviour
{
    [Header("Refs")]
    public EnemyAI enemy;                 // drag Om Agus (EnemyAI)
    public Transform player;              // drag Player / kamera
    public CanvasGroup vignetteGroup;     // Panel UI full screen (alpha dikontrol)
    public CameraShaker shaker;           // di MainCamera
    public AudioSource heartbeatSource;   // AudioSource 2D utk heartbeat SFX

    [Header("Vignette (base)")]
    [Range(0f, 1f)] public float maxVignette = 0.6f;   // plafon gelap
    public float fadeIn = 0.25f;                        // saat mulai dikejar
    public float fadeOut = 0.35f;                       // saat lepas

    [Header("Proximity")]
    public float minDist = 1.5f;                        // sangat dekat = intensitas 1
    public float maxDist = 12f;                         // di atas ini = 0
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0,0, 1,1); // 0..1 jarak -> 0..1 intens

    [Header("Vignette Pulse (kelap-kelip)")]
    public bool pulse = true;
    [Range(0f, 2f)] public float pulseHz = 0.8f;        // frekuensi denyut (0.8 ≈ pelan)
    [Range(0f, 1f)] public float pulseAmount = 0.2f;    // porsi tambahan dari base intensity
    [Range(0f, 1f)] public float pulseThreshold = 0.3f; // mulai berdenyut jika sudah cukup gelap
    public bool syncToHeartbeat = true;                 // ikut pitch SFX heartbeat
    [Range(0f, 1f)] public float heartbeatInfluence = 0.5f;

    [Header("Smoothing")]
    public float vignetteLerpSpeed = 5f;                // smoothing kecil biar mulus (alpha)

    [Header("Shake mapping")]
    public float shakeAtMin = 1f;                       // intensitas shake saat paling dekat
    public float shakeAtMax = 0f;                       // intensitas ketika jauh

    [Header("Heartbeat mapping")]
    [Range(0f,1f)] public float hbVolMin = 0f;
    [Range(0f,1f)] public float hbVolMax = 0.9f;
    public float hbPitchMin = 0.9f;
    public float hbPitchMax = 1.25f;

    float targetActive;   // 0/1 target aktif
    float activeBlend;    // 0..1 smooth on/off
    Transform enemyTr;

    void Awake()
    {
        if (!enemy) enemy = FindObjectOfType<EnemyAI>(true);
        if (!player) player = Camera.main ? Camera.main.transform : null;
        enemyTr = enemy ? enemy.transform : null;

        if (vignetteGroup) vignetteGroup.alpha = 0f;

        if (heartbeatSource)
        {
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        // Auto-play heartbeat dalam keadaan mute agar tidak ada delay saat volume naik
        if (heartbeatSource && !heartbeatSource.isPlaying)
        {
            heartbeatSource.volume = 0f;
            heartbeatSource.pitch = 1f;
            heartbeatSource.Play();
        }
    }

    // Dipanggil dari EnemyAI
    public void OnChaseStart() => targetActive = 1f;
    public void OnChaseEnd()   => targetActive = 0f;

    void Update()
    {
        // Smooth on/off blend
        float speed = (targetActive > activeBlend)
            ? (1f / Mathf.Max(0.01f, fadeIn))
            : (1f / Mathf.Max(0.01f, fadeOut));
        activeBlend = Mathf.MoveTowards(activeBlend, targetActive, Time.deltaTime * speed);

        // --- Hitung intensitas berbasis jarak 0..1 ---
        float prox = 0f;
        if (enemyTr && player)
        {
            float d = Vector3.Distance(enemyTr.position, player.position);
            float t = Mathf.InverseLerp(maxDist, minDist, d); // jauh=0, dekat=1
            prox = intensityCurve.Evaluate(Mathf.Clamp01(t));
        }

        // Base intensity (0..1), digate oleh aktif/tidak
        float baseIntensity = activeBlend * prox;

        // Base alpha (0..maxVignette)
        float baseAlpha = Mathf.Clamp01(baseIntensity) * maxVignette;

        // --- Pulse (kelap-kelip pelan) ---
        float pulseExtra = 0f;
        if (pulse && baseAlpha > pulseThreshold * maxVignette)
        {
            float hz = pulseHz;
            if (syncToHeartbeat && heartbeatSource && heartbeatSource.isPlaying)
                hz = Mathf.Lerp(hz, heartbeatSource.pitch, heartbeatInfluence);

            // gelombang 0..1 pakai waktu unscaled (tak terpengaruh pause/slowmo)
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * hz);

            // tambahan proporsional terhadap baseAlpha supaya natural
            pulseExtra = wave * pulseAmount * baseAlpha;
        }

        // Target alpha akhir dan apply dengan smoothing kecil
        float targetAlpha = Mathf.Clamp01(baseAlpha + pulseExtra);
        if (vignetteGroup)
        {
            float lerpSpd = Mathf.Max(0f, vignetteLerpSpeed);
            vignetteGroup.alpha = (lerpSpd > 0f)
                ? Mathf.MoveTowards(vignetteGroup.alpha, targetAlpha, Time.unscaledDeltaTime * lerpSpd)
                : targetAlpha;
        }

        // Shake kamera mengikuti intensitas total (bukan base saja)
        if (shaker)
        {
            float totalIntensity01 = Mathf.Clamp01((targetAlpha / Mathf.Max(0.0001f, maxVignette)));
            float shake = Mathf.Lerp(shakeAtMax, shakeAtMin, totalIntensity01);
            shaker.SetShake(shake);
        }

        // Heartbeat mengikuti base intensity (stabil)
        if (heartbeatSource)
        {
            heartbeatSource.volume = Mathf.Lerp(hbVolMin, hbVolMax, baseIntensity);
            heartbeatSource.pitch  = Mathf.Lerp(hbPitchMin, hbPitchMax, baseIntensity);
        }
    }
}
