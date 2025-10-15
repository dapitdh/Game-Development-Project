using UnityEngine;

public class Sc_paku : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    bool moving = false;
    private Vector3 targetPos;
    public Sc_kayuPenghalang sc_kayuPenghalang;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(moving)
        {
            this.transform.position = Vector3.MoveTowards(
                this.transform.position,
                targetPos,
                2f * Time.deltaTime
            );
            if (this.transform.position == targetPos)
            {
                moving = false;
                this.GetComponent<Rigidbody>().isKinematic = false;
                if (this.name == "paku")
                {
                    sc_kayuPenghalang.RemoveLeftNail();
                } else if (this.name == "paku (1)")
                {
                    sc_kayuPenghalang.RemoveRightNail();
                }
                
            }
                
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.name == "crowbar (1)")
        {
            targetPos = this.transform.position + new Vector3(0, 0, -0.1f);
            moving = true;
        }
    }
}
