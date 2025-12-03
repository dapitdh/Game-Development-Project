using UnityEngine;
using TMPro;

public class Sc_pintu : MonoBehaviour
{
    public float speed = 3f;
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isHeroNear = false;
    private bool isBlocked = false; // untuk mendeteksi jika ada item yang menghalangi pintu
    public GameObject leftClickGUI; // UI left click
    public TextMeshProUGUI text; // Text "ext" untuk menghilangkan teks saat tidak dekat pintu
    void Start()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(transform.localEulerAngles + new Vector3(0, 95, 0));
    }

    void Update()
    {

        // if(isHeroNear && !isBlocked)
        //     leftClickGUI.SetActive(true);
        if (Input.GetKeyDown(KeyCode.Mouse0) && isHeroNear && !isBlocked)
        {
            isOpen = !isOpen;
        }
        // else if (!isHeroNear)
        //     leftClickGUI.SetActive(false);


        Quaternion targetRot = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if(!enabled) return;
        if (other.CompareTag("Obstacle") || other.CompareTag("KayuPenghalang"))
        {
            isBlocked = true;
            leftClickGUI.SetActive(false);
            Debug.Log("Pintu terhalang: " + isBlocked);
        }
        if (other.CompareTag("Player"))
        {
            isHeroNear = true;
            if (!isBlocked)
            {
                text.text = isOpen ? "Close Door" : "Open Door";
                leftClickGUI.SetActive(true);
            }

            Debug.Log("Hero dekat pintu: " + isHeroNear);
        }


    }

    void OnTriggerExit(Collider other)
    {
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;
        if (other.CompareTag("Player"))
            isHeroNear = false;

        leftClickGUI.SetActive(false);

        if (other.CompareTag("Obstacle") || other.CompareTag("KayuPenghalang"))
        {
            isBlocked = false;
            Debug.Log("Pintu terhalang: " + isBlocked);
        }
    }

    // ========= API publik untuk AI =========

    // status
    public bool IsOpen => isOpen;
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