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
    public GameObject bulletPrefab;         
    public float bulletRadius = 0.05f;      
    public float bulletSpeed = 50f;
    public float bulletLifetime = 5f;
    public bool bulletUseGravity = false;
    public LayerMask bulletLayer = 0;

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

    Transform holder;      
    bool isHeld;
    float nextShootAllowedTime;
    float reloadReadyTime;       
    int ammo;                   
    public void OnPickedUp(Transform itemHolder)
    {
        holder = itemHolder;
        isHeld = true;

        if (ammo <= 0)
        {
            if (Time.time >= reloadReadyTime) Reload();
            else nextShootAllowedTime = Mathf.Max(nextShootAllowedTime, reloadReadyTime);
        }
    }

    public void OnDropped()
    {
        isHeld = false;
        holder = null;
    }

    void Awake()
    {
        ammo = magazineSize;
        nextShootAllowedTime = 0f;
        reloadReadyTime = 0f;
    }

    void Update()
    {
        if (!isHeld) return;

        if (ammo == 0 && reloadReadyTime > 0f && Time.time >= reloadReadyTime)
        {
            Reload();
        }

        bool wantShoot = Mouse.current?.leftButton.isPressed ?? false; 

        if (!wantShoot) return;

        if (CanShoot())
        {
            FireOnce();
            ammo--;
            if (ammo > 0)
            {
                nextShootAllowedTime = Time.time + shortCooldown;
            }
            else
            {
                nextShootAllowedTime = Time.time + longCooldown;
                reloadReadyTime = nextShootAllowedTime; 
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
    }

    void FireOnce()
    {
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

        GameObject bullet = CreateBullet(spawnPos);
        if (!bullet) return;

        if (bulletLayer.value != 0)
            bullet.layer = LayerMaskToLayer(bulletLayer);

        if (bullet.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = dir * bulletSpeed;

        IgnoreCollisionWithHolder(bullet);

        Destroy(bullet, bulletLifetime);

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

        var cols = holder.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
            if (c) Physics.IgnoreCollision(bulletCol, c, true);

        var selfCols = GetComponentsInChildren<Collider>(true);
        foreach (var c in selfCols)
            if (c) Physics.IgnoreCollision(bulletCol, c, true);
    }

    int LayerMaskToLayer(LayerMask mask)
    {
        int value = mask.value;
        for (int i = 0; i < 32; i++)
            if (value == (1 << i)) return i;
        return gameObject.layer;
    }
}
