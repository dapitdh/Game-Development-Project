using UnityEngine;

public class Sc_kayuPenghalang : MonoBehaviour
{
    [Header("Anchors (letakkan di ujung papan)")]
    public Transform leftAnchor;
    public Transform rightAnchor;

    [Header("Arah engsel (biasanya Z untuk dinding datar)")]
    public Vector3 hingeAxis = new Vector3(1, 0, 0);

    Rigidbody rb;
    HingeJoint activeHinge;

    bool leftNail = true;
    bool rightNail = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyState(); // awal: dua paku terpasang
    }

    // --- Panggil fungsi ini saat “mencabut paku” ---
    public void RemoveLeftNail()
    {
        if (!leftNail) return;
        leftNail = false;
        ApplyState();
    }

    public void RemoveRightNail()
    {
        if (!rightNail) return;
        rightNail = false;
        ApplyState();
    }

    // --- Panggil ini kalau mau pasang kembali paku (opsional) ---
    public void RestoreLeftNail()  { leftNail  = true; ApplyState(); }
    public void RestoreRightNail() { rightNail = true; ApplyState(); }

    void ApplyState()
    {
        // Bersihkan hinge lama
        if (activeHinge) Destroy(activeHinge);
        activeHinge = null;

        // Kasus 1: dua paku terpasang -> kunci papan (tanpa hinge)
        if (leftNail && rightNail)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation; // biar kaku di tempat
            rb.constraints |= RigidbodyConstraints.FreezePositionX;
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            rb.constraints |= RigidbodyConstraints.FreezePositionZ;
            return;
        }

        // Kasus 2: hanya satu paku tersisa -> buat hinge di anchor paku yang tersisa
        if (leftNail ^ rightNail) // XOR: tepat satu true
        {
            rb.constraints = RigidbodyConstraints.None;
            var anchor = leftNail ? leftAnchor : rightAnchor;

            activeHinge = gameObject.AddComponent<HingeJoint>();
            activeHinge.connectedBody = null; // ke dunia (world), bisa juga sambungkan ke Rigidbody dinding jika ada
            activeHinge.autoConfigureConnectedAnchor = false;

            // Anchor lokal pada papan
            activeHinge.anchor = transform.InverseTransformPoint(anchor.position);
            // ConnectedAnchor di world-space (karena connectedBody = null)
            activeHinge.connectedAnchor = anchor.position;

            // Atur axis (lokal papan)
            activeHinge.axis = hingeAxis.normalized;

            // Bebas berputar (tanpa limit). Kalau mau ada limit:
            // activeHinge.useLimits = true;
            // var lim = activeHinge.limits; lim.min = -110f; lim.max = 110f; activeHinge.limits = lim;

            return;
        }

        // Kasus 3: dua-duanya lepas -> jatuh bebas
        rb.constraints = RigidbodyConstraints.None;
    }
}
