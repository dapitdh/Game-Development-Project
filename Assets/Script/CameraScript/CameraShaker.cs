using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity;  // 0..1 dikirim dari FX
    public float posAmplitude = 0.08f;       // meter
    public float rotAmplitude = 1f;        // derajat
    public float freq = 12f;

    Vector3 baseLocalPos;
    Quaternion baseLocalRot;
    float t;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;
    }

    void OnEnable()
    {
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;
        t = Random.value * 100f;
    }

    public void SetShake(float v) => intensity = Mathf.Clamp01(v);

    void LateUpdate()
    {
        if (intensity <= 0.0001f)
        {
            transform.localPosition = baseLocalPos;
            transform.localRotation = baseLocalRot;
            return;
        }

        t += Time.deltaTime * Mathf.Lerp(0.5f, 1.5f, intensity) * freq;

        float nx = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0.13f, t) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(t, t) - 0.5f) * 2f;

        Vector3 offset = new Vector3(nx, ny, 0f) * posAmplitude * intensity;
        Vector3 euler = new Vector3(ny, 0f, -nx) * rotAmplitude * intensity;

        transform.localPosition = baseLocalPos + offset;
        transform.localRotation = baseLocalRot * Quaternion.Euler(euler);
    }
}
