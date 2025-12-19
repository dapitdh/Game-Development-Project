using UnityEngine;
using FPP;
using TMPro;

public class Sc_showUIPin : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] GameObject pinUI;
    GameObject hero;
    private bool isSolved = false;
    public bool isClicked = false;
    private bool isNear = false;
    [SerializeField] private GameObject leftClickGUI; // UI left click
    [SerializeField] private TextMeshProUGUI text;

    void Start()
    {
        hero = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (isNear && isClicked && !isSolved)
        {
            showPinUI();
        }
    }
    private void showPinUI()
    {
        pinUI.SetActive(true);
        leftClickGUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;   // lepas kunci
        Cursor.visible = true;
        hero.transform.GetComponent<Animator>().SetBool("walk", false);
        hero.transform.GetComponent<Puan_control>().enabled = false;
        hero.transform.GetComponentInChildren<FPP_CameraControl>().enabled = false;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == hero && !isSolved)
        {
            isNear = true;
            text.text = "Enter Pin";
            leftClickGUI.SetActive(true);
        }

    }
    public void ClosePinUI(bool solved)
    {
        Debug.Log("Closing PIN UI");
        isClicked = false;
        pinUI.SetActive(false);
        pinUI.GetComponent<Sc_pin>().ResetPin(); // reset PIN saat ditutup
        Cursor.lockState = CursorLockMode.Locked;   // kunci lagi
        Cursor.visible = false;
        hero.transform.GetComponent<Puan_control>().enabled = true;
        hero.transform.GetComponentInChildren<FPP_CameraControl>().enabled = true;
        isSolved = solved;
    }
}
