using UnityEngine;

public class Sc_pintuKulkas : MonoBehaviour
{
    public float openAngle = -90f;  // negatif = buka ke kiri, positif = ke kanan
    public float speed = 3f;
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private bool isHeroNear = false;

    void Start()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(transform.localEulerAngles + new Vector3(0, 90, 0));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isHeroNear)
            isOpen = !isOpen;

        Quaternion targetRot = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hero"))
            isHeroNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hero"))
            isHeroNear = false;
    }
}