using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class WeaponShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ujung laras. Kalau null, pakai kamera untuk arah & posisi fallback.")]
    public Transform muzzle;

    [Header("Bullet (Sphere)")]
    [Tooltip("Kosongkan untuk auto-buat sphere runtime.")]
    public GameObject bulletPrefab;          // opsional
    public float bulletRadius = 0.05f;       // 5 cm
    public float bulletSpeed = 50f;
    public float bulletLifetime = 5f;
    public bool bulletUseGravity = false;
    public LayerMask bulletLayer = 0;        // opsional: set layer peluru

    [Header("Cooldown & Ammo Rules")]
    [Tooltip("Jumlah tembakan per isi.")]
    public int magazineSize = 2;
    [Tooltip("Cooldown setelah 1 tembakan (detik).")]
    public float shortCooldown = 2f;
    [Tooltip("Long cooldown setelah magazine habis (detik).")]
    public float longCooldown = 20f;

    [Header("FX (MuzzleFlash Cara A)")]
    [Tooltip("ParticleSystem yang nempel di Muzzle. Akan di-Play saat tembak.")]
    public ParticleSystem muzzleFlashPS;
    [Tooltip("AudioSource opsional (di pistol).")]
    public AudioSource fireSfx;
    [Tooltip("Clip tembakan (dipakai oleh AudioSource).")]
    public AudioClip fireClip;

    // ===== runtime state =====
    Transform holder;        // itemHolder dari player
    bool isHeld;
    float nextShootAllowedTime;  // kapan boleh menembak lagi
    float reloadReadyTime;       // kapan reload otomatis selesai (untuk long cooldown)
    int ammo;                    // sisa peluru di magazine

    // Dipanggil saat di-pickup oleh script pickup
    public void OnPickedUp(Transform itemHolder)
    {
        holder = itemHolder;
        isHeld = true;

        // Jika baru dipegang & belum ada ammo, inisialisasi atau cek reload
        if (ammo <= 0)
        {
            if (Time.time >= reloadReadyTime) Reload();
            else nextShootAllowedTime = Mathf.Max(nextShootAllowedTime, reloadReadyTime);
        }
    }

    // Dipanggil saat drop
    public void OnDropped()
    {
        isHeld = false;
        holder = null;
    }

    void Awake()
    {
        // start penuh
        ammo = magazineSize;
        nextShootAllowedTime = 0f;
        reloadReadyTime = 0f;
    }

    void Update()
    {
        if (!isHeld) return;

        // Selesaikan reload otomatis (setelah long cooldown)
        if (ammo == 0 && reloadReadyTime > 0f && Time.time >= reloadReadyTime)
        {
            Reload();
        }

        bool wantShoot = Mouse.current?.leftButton.isPressed ?? false; // tahan = oke, kita kunci via cooldown

        if (!wantShoot) return;

        if (CanShoot())
        {
            FireOnce();

            // Aturan cooldown:
            ammo--;
            if (ammo > 0)
            {
                // Short cooldown (2 detik default)
                nextShootAllowedTime = Time.time + shortCooldown;
            }
            else
            {
                // Magazine habis → long cooldown (20 detik default) + jadwalkan reload
                nextShootAllowedTime = Time.time + longCooldown;
                reloadReadyTime = nextShootAllowedTime; // reload tepat saat long cooldown selesai
            }
        }
    }

    bool CanShoot()
    {
        if (!isHeld) return false;
        if (ammo <= 0) return false;
        if (Time.time < nextShootAllowedTime) return false;
        return true;
    }

    void Reload()
    {
        ammo = magazineSize;
        reloadReadyTime = 0f;
        // (opsional) mainkan SFX/anim reload di sini
        // Debug.Log("Reload selesai, ammo = " + ammo);
    }

    void FireOnce()
    {
        // Tentukan asal & arah tembakan
        Transform cam = Camera.main ? Camera.main.transform : null;

        Vector3 spawnPos;
        Vector3 dir;

        if (muzzle != null)
        {
            spawnPos = muzzle.position + muzzle.forward * 0.1f;
            dir = (cam != null ? cam.forward : muzzle.forward).normalized;
        }
        else if (cam != null)
        {
            spawnPos = cam.position + cam.forward * 0.6f;
            dir = cam.forward.normalized;
        }
        else
        {
            spawnPos = transform.position + transform.forward * 0.1f;
            dir = transform.forward.normalized;
        }

        // Buat peluru
        GameObject bullet = CreateBullet(spawnPos);
        if (!bullet) return;

        // Set layer peluru (opsional)
        if (bulletLayer.value != 0)
            bullet.layer = LayerMaskToLayer(bulletLayer); // first set bit

        // Dorong peluru
        if (bullet.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = dir * bulletSpeed;

        // Abaikan tabrakan peluru dengan pemain/senjata
        IgnoreCollisionWithHolder(bullet);

        // Auto-destroy
        Destroy(bullet, bulletLifetime);

        // === FX ===
        if (muzzleFlashPS) muzzleFlashPS.Play(true);
        if (fireSfx && fireClip) fireSfx.PlayOneShot(fireClip);
        else if (fireSfx) fireSfx.Play();
    }

    GameObject CreateBullet(Vector3 pos)
    {
        GameObject go;

        if (bulletPrefab != null)
        {
            go = Instantiate(bulletPrefab, pos, Quaternion.identity);
            EnsureBulletComponents(go);
            return go;
        }

        // Buat sphere runtime
        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetPositionAndRotation(pos, Quaternion.identity);
        go.transform.localScale = Vector3.one * (bulletRadius * 2f);

        var col = go.GetComponent<SphereCollider>();
        if (col) col.isTrigger = false;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = bulletUseGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (!go.TryGetComponent<SphereBullet>(out _))
            go.AddComponent<SphereBullet>();

        return go;
    }

    void EnsureBulletComponents(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (!col) col = go.AddComponent<SphereCollider>();
        col.isTrigger = false;

        var rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = bulletUseGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (!go.TryGetComponent<SphereBullet>(out _))
            go.AddComponent<SphereBullet>();
    }

    void IgnoreCollisionWithHolder(GameObject bullet)
    {
        if (!holder) return;

        var bulletCol = bullet.GetComponent<Collider>();
        if (!bulletCol) return;

        // Abaikan semua collider di holder (player + senjata)
        var cols = holder.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
            if (c) Physics.IgnoreCollision(bulletCol, c, true);

        // Abaikan collider senjata ini sendiri
        var selfCols = GetComponentsInChildren<Collider>(true);
        foreach (var c in selfCols)
            if (c) Physics.IgnoreCollision(bulletCol, c, true);
    }

    // Ambil index layer dari LayerMask (jika hanya 1 bit)
    int LayerMaskToLayer(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
            if (value == (1 << i)) return i;
        return gameObject.layer; // fallback
    }
}
