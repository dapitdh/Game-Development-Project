using UnityEngine;

public class Sc_win : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject winUI, player, objectiveUI;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        winUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("You Win!");
            winUI.SetActive(true);
            objectiveUI.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // player.GetComponent<Puan_control>().enabled = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
            // Tambahkan logika kemenangan di sini, seperti menampilkan UI kemenangan atau memuat level berikutnya
        }
    }
}
