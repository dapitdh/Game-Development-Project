using UnityEngine;

public class Sc_pintu : MonoBehaviour
{
    public float speed = 3f;
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isHeroNear = false;
    private bool isBlocked = false; // untuk mendeteksi jika ada item yang menghalangi pintu

    void Start()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(transform.localEulerAngles + new Vector3(0, 95, 0));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isHeroNear && !isBlocked)
            isOpen = !isOpen;

        Quaternion targetRot = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHeroNear = true;
            Debug.Log("Hero dekat pintu: " + isHeroNear);
        }

        if (other.CompareTag("Obstacle"))
        {
            isBlocked = true;
            Debug.Log("Pintu terhalang: " + isBlocked);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        if (other.CompareTag("Player"))
            isHeroNear = false;

        if (other.CompareTag("Obstacle"))
        {
            isBlocked = false;
            Debug.Log("Pintu terhalang: " + isBlocked);
        }
    }

    // ========= API publik untuk AI =========

    // status
    public bool IsOpen   => isOpen;
    public bool IsBlocked => isBlocked;

    // 0..1 kira-kira seberapa “terbuka” (berdasarkan sudut ke openRot)
    public float OpenPercent
    {
        get
        {
            float ang = Quaternion.Angle(transform.localRotation, openRot); // 0 saat pas openRot
            float full = Mathf.Max(1f, Quaternion.Angle(closedRot, openRot)); // total sudut
            float p = 1f - Mathf.Clamp01(ang / full);
            return p;
        }
    }

    // minta buka/tutup secara programatik (tetap diproses oleh Update melalui Lerp)
    public void Open()
    {
        if (!isBlocked) isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }
}