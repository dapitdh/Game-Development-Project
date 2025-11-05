using UnityEngine;
using TMPro;

public class Sc_pintuLemari : MonoBehaviour
{
    [Header("Kecepatan buka pintu")]
    public float speed = 3f;

    [Header("Rotasi pintu kiri saat terbuka (X, Y, Z)")]
    public Vector3 rotasiBukaKiri = new Vector3(0, 85, 0);

    [Header("Rotasi pintu kanan saat terbuka (X, Y, Z)")]
    public Vector3 rotasiBukaKanan = new Vector3(0, -85, 0);

    private bool isOpen = false;
    private bool isHeroNear = false;

    private Quaternion closedRotKiri, closedRotKanan;
    private Quaternion openRotKiri, openRotKanan;

    private GameObject pintuKiri, pintuKanan;
    public GameObject leftClickGUI; // UI left click
    public TextMeshProUGUI text;

    void Start()
    {
        // Temukan pintu di dalam parent
        pintuKiri = transform.Find("pintuKiri").gameObject;
        pintuKanan = transform.Find("pintuKanan").gameObject;

        // Simpan posisi rotasi awal (tertutup)
        closedRotKiri = pintuKiri.transform.localRotation;
        closedRotKanan = pintuKanan.transform.localRotation;

        // Rotasi target (terbuka)
        openRotKiri = Quaternion.Euler(pintuKiri.transform.localEulerAngles + rotasiBukaKiri);
        openRotKanan = Quaternion.Euler(pintuKanan.transform.localEulerAngles + rotasiBukaKanan);
    }

    void Update()
    {
        if (isHeroNear && Input.GetKeyDown(KeyCode.Mouse0))
            isOpen = !isOpen;

        // Tentukan rotasi target
        Quaternion targetRotKiri = isOpen ? openRotKiri : closedRotKiri;
        Quaternion targetRotKanan = isOpen ? openRotKanan : closedRotKanan;

        // Lerp ke posisi target
        pintuKiri.transform.localRotation = Quaternion.Lerp(pintuKiri.transform.localRotation, targetRotKiri, Time.deltaTime * speed);
        pintuKanan.transform.localRotation = Quaternion.Lerp(pintuKanan.transform.localRotation, targetRotKanan, Time.deltaTime * speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isHeroNear = true;
            leftClickGUI.SetActive(true);
            text.text = isOpen ? "Close Door" : "Open Door";
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            leftClickGUI.SetActive(false);
            isHeroNear = false;
            text.text = isOpen ? "Close Door" : "Open Door";
        }
    }
}
