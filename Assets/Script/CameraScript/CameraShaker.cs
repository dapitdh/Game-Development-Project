using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;   // 0..1 dikirim dari FX
    public float posAmplitude = 0.08f;        // meter
    public float rotAmplitude = 1f;           // derajat
    public float freq = 12f;

    [Header("Optional")]
    public Transform target; // transform yang digoyang (disarankan: child/pivot khusus)

    float t;

    // kita simpan offset yang TERAKHIR diaplikasikan supaya bisa di-undo tiap frame
    Vector3 lastPosOffset = Vector3.zero;
    Quaternion lastRotOffset = Quaternion.identity;

    void Awake()
    {
        if (target == null) target = transform;
        t = Random.value * 100f;
    }

    void OnEnable()
    {
        // reset offset agar aman saat enable
        lastPosOffset = Vector3.zero;
        lastRotOffset = Quaternion.identity;
        t = Random.value * 100f;
    }

    void OnDisable()
    {
        // bersihkan shake terakhir supaya kamera tidak “nyangkut” dalam keadaan goyang
        UndoLastShake();
    }

    public void SetShake(float v) => intensity = Mathf.Clamp01(v);

    void LateUpdate()
    {
        // 1) undo shake frame sebelumnya -> balik ke state kamera hasil mouse look
        UndoLastShake();

        if (intensity <= 0.0001f) return;

        // 2) hitung noise
        t += Time.deltaTime * Mathf.Lerp(0.5f, 1.5f, intensity) * freq;

        float nx = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0.13f, t) - 0.5f) * 2f;

        // 3) buat offset shake (pos + rot)
        Vector3 posOffset = new Vector3(nx, ny, 0f) * posAmplitude * intensity;

        // pitch (X) + roll (Z). Yaw sengaja 0 biar arah pandang tidak “lari”
        Vector3 euler = new Vector3(ny, 0f, -nx) * rotAmplitude * intensity;
        Quaternion rotOffset = Quaternion.Euler(euler);

        // 4) apply additive di atas transform yang sudah diputar oleh mouse look
        target.localPosition += posOffset;
        target.localRotation = target.localRotation * rotOffset;

        // 5) simpan untuk di-undo next frame
        lastPosOffset = posOffset;
        lastRotOffset = rotOffset;
    }

    void UndoLastShake()
    {
        if (target == null) return;

        if (lastPosOffset != Vector3.zero)
            target.localPosition -= lastPosOffset;

        if (lastRotOffset != Quaternion.identity)
            target.localRotation = target.localRotation * Quaternion.Inverse(lastRotOffset);

        lastPosOffset = Vector3.zero;
        lastRotOffset = Quaternion.identity;
    }
}
