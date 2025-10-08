using UnityEngine;

public class Sc_pintuLemari : MonoBehaviour
{
    public float speed = 3f;
    private bool isOpen = false;
    private Quaternion closedRotKiri, closedRotKanan;
    private Quaternion openRotKiri, openRotKanan;
    private bool isHeroNear = false;
    GameObject pintuKiri, pintuKanan;

    void Start()
    {
        pintuKiri = transform.Find("pintuKiri").gameObject;
        pintuKanan = transform.Find("pintuKanan").gameObject;
        closedRotKiri = pintuKiri.transform.localRotation;
        closedRotKanan = pintuKanan.transform.localRotation;
        openRotKiri = Quaternion.Euler(pintuKiri.transform.localEulerAngles + new Vector3(0, 85, 4));
        openRotKanan = Quaternion.Euler(pintuKanan.transform.localEulerAngles + new Vector3(5, -102, -1));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isHeroNear)
            isOpen = !isOpen;

        Quaternion targetRotKiri = isOpen ? openRotKiri : closedRotKiri;
        Quaternion targetRotKanan = isOpen ? openRotKanan : closedRotKanan;

        pintuKiri.transform.localRotation = Quaternion.Lerp(pintuKiri.transform.localRotation, targetRotKiri, Time.deltaTime * speed);
        pintuKanan.transform.localRotation = Quaternion.Lerp(pintuKanan.transform.localRotation, targetRotKanan, Time.deltaTime * speed);
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