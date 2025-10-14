using UnityEngine;

public class Sc_camera : MonoBehaviour
{
    Transform target;
    Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("char_puan").transform;
        offset = new Vector3(target.position.x, target.position.y + 1, target.position.z - 3);
        //offset = this.transform.position - target.position;
    }

    // Update is called once per frame
    void Update()
    {
        offset = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 3f, Vector3.up) * offset;
        this.transform.position = target.position + offset;
        this.transform.LookAt(target);
    }
}
