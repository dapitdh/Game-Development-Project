using UnityEngine;

public class Sc_win : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject winUI, player, objectiveUI, objectiveUI2;
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
            objectiveUI2.SetActive(true);
        }
    }
}
