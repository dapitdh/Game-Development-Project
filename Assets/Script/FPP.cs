using UnityEngine;

public class FPP : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    float rotationVertical = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Rotation Horizontal
        float rotationHorizontal = Input.GetAxis("Mouse X") * 3f + transform.localEulerAngles.y;
        transform.localEulerAngles= new Vector3(0, rotationHorizontal, 0);

        //Rotation Vertical
        rotationVertical += Input.GetAxis("Mouse Y") * 3f;
        rotationVertical = Mathf.Clamp(rotationVertical, -60, 60);
        transform.localEulerAngles= new Vector3(-rotationVertical, transform.localEulerAngles.y, 0);

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(h, 0, v) * Time.deltaTime * 3f);
    }
}
