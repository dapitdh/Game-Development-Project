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
            isHeroNear = true;
            Debug.Log("Hero dekat pintu: " + isHeroNear);

        if (other.CompareTag("Item"))
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

        if (other.CompareTag("Item"))
        {
            isBlocked = false;
            Debug.Log("Pintu terhalang: " + isBlocked);
        }
    }
}