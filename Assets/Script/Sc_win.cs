using UnityEngine;

public class Sc_win : MonoBehaviour
{
    public GameObject winUI, player, objectiveUI, objectiveUI2;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

    }

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
