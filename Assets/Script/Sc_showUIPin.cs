using UnityEngine;
using FPP;

public class Sc_showUIPin : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] GameObject pinUI;
    GameObject hero;
    void Start()
    {
        hero = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == hero)
        {
            Debug.Log("Masuk area pin");
            pinUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;   // lepas kunci
            Cursor.visible = true;
            hero.transform.GetComponent<Puan_control>().enabled = false;
            hero.transform.GetComponentInChildren<FPP_CameraControl>().enabled = false;
        }
    }
}
