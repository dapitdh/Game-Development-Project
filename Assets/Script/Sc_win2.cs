using UnityEngine;

public class Sc_win2 : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject player, objectiveUI, objectiveUI2, objectiveUI3;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            objectiveUI.SetActive(false);
            objectiveUI2.SetActive(false);
            objectiveUI3.SetActive(true);
        }
    }
}
